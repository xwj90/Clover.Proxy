using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Clover.Proxy.OldDesign;
using Microsoft.CSharp;

namespace Clover.Proxy
{
    internal class DefaultProxyProvider : ProxyProviderBase
    {
        private readonly string DLLCachedPath = AppDomain.CurrentDomain.BaseDirectory;
        private static ConcurrentDictionary<Type, Assembly> assemblies = new ConcurrentDictionary<Type, Assembly>();

        public DefaultProxyProvider(ProxyConfiguration config)
            : base(config)
        {
            this.DLLCachedPath = config.DllCachedPath;
        }

        public override T CreateInstance<T>()
        {
            if (proxyConfig.DisableAutoProxy)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            var type = typeof(T);
            var assembly = assemblies.GetOrAdd(type, (t) => { return CreateLocalAssembly<T>(type.Assembly); });
            Type proxyType = assembly.GetType(TypeInformation.GetLocalProxyClassFullName(type));
            return (T)Activator.CreateInstance(proxyType, new Object[] { this });
        }

        private List<MethodInfo> FindAllMethods(Type type)
        {
            if (proxyConfig.DisableAutoProxy) return new List<MethodInfo>();
            var methodInfoList = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.IsVirtual && !p.IsSpecialName);
            var resultList = new List<MethodInfo>();
            foreach (var methodInfo in methodInfoList)
            {
                bool status;
                if (proxyConfig.MemberAutoProxyStatus.TryGetValue(methodInfo.Name, out status))
                {
                    if (status) resultList.Add(methodInfo);
                }
                else { resultList.Add(methodInfo); }
            }
            return resultList;
        }
        private List<PropertyInfo> FindAllProperties(Type type)
        {
            if (proxyConfig.DisableAutoProxy) return new List<PropertyInfo>();
            var propertyInfoList = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.GetGetMethod().IsVirtual).ToList();
            var resultList = new List<PropertyInfo>();
            foreach (var propertyInfo in propertyInfoList)
            {
                bool status;
                if (proxyConfig.MemberAutoProxyStatus.TryGetValue(propertyInfo.Name, out status))
                {
                    if (status) resultList.Add(propertyInfo);
                }
                else { resultList.Add(propertyInfo); }
            }
            return resultList;
        }

        private Assembly CreateLocalAssembly<T>(Assembly entityAssembly)
        {
            Type type = typeof(T);
            string localClassName = TypeInformation.GetLocalProxyClassName(type);

            var codeUnit = new CodeCompileUnit();
            var codeNamespace = CreateNSAndImportInitNS(type, codeUnit);

            var wrapProxyClass = new CodeTypeDeclaration(localClassName);
            wrapProxyClass.BaseTypes.Add(type);
            //todo:must be Serializable?
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            codeNamespace.Types.Add(wrapProxyClass);

            var providerBaseField = new CodeMemberField(typeof(ProxyProviderBase), "_proxyProviderBase");
            wrapProxyClass.Members.Add(providerBaseField);
            var baseTypeField = new CodeMemberField(typeof(Type), "_proxyBaseType");
            wrapProxyClass.Members.Add(baseTypeField);

            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ProxyProviderBase), "_proxyProviderBase"));
            constructor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("this._proxyProviderBase"), new CodeVariableReferenceExpression("_proxyProviderBase")));
            constructor.Statements.Add(new CodeVariableReferenceExpression("this._proxyBaseType = this.GetType().BaseType;"));
            wrapProxyClass.Members.Add(constructor);

            OverrideMethods(type, wrapProxyClass);
            OverrideProperties(type, wrapProxyClass);

            var cprovider = new CSharpCodeProvider();
            var fileContent = new StringBuilder();
            using (var sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(codeUnit, sw, new CodeGeneratorOptions());
            }

            var compilerParameters = CreateCompilerParameters(type);

            string filePath = DLLCachedPath + @"Class\" + type.Namespace + "." + type.Name + ".Local.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, fileContent.ToString());

            CompilerResults cr = cprovider.CompileAssemblyFromFile(compilerParameters, filePath);

            String outputMessage = "";
            foreach (string item in cr.Output)
            {
                outputMessage += item + Environment.NewLine;
            }
            if (cr.Errors.Count > 0)
            {
                throw new Exception("complie local proxy class error:" + Environment.NewLine + outputMessage);
            }
            return cr.CompiledAssembly;
        }

        private CompilerParameters CreateCompilerParameters(Type proxyedType)
        {
            var compilerParameters = new CompilerParameters();
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            compilerParameters.ReferencedAssemblies.Add(DLLCachedPath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            compilerParameters.ReferencedAssemblies.Add(DLLCachedPath + Path.GetFileName(proxyedType.Assembly.Location));
            //compilerParameters.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));

            //foreach (string file in Directory.GetFiles(DLLCachedPath, "*.dll"))
            //{
            //    if (file.ToUpper().StartsWith("Clover."))
            //        continue;
            //    compilerParameters.ReferencedAssemblies.Add(file);
            //}

            compilerParameters.OutputAssembly = DLLCachedPath + proxyedType.FullName + ".Local.dll";
            compilerParameters.GenerateInMemory = false;
            compilerParameters.IncludeDebugInformation = true;
            compilerParameters.GenerateExecutable = false; //生成EXE,不是DLL 
            compilerParameters.WarningLevel = 4;
            compilerParameters.TreatWarningsAsErrors = false;
            return compilerParameters;
        }

        private CodeNamespace CreateNSAndImportInitNS(Type currentType, CodeCompileUnit codeUnit)
        {
            var codeNamespace = new CodeNamespace(TypeInformation.GetLocalNamespace(currentType));
            codeUnit.Namespaces.Add(codeNamespace);

            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            //foreach (Type item in InterfaceAssembly.GetTypes())
            //{
            //    codeNamespace.Imports.Add(new CodeNamespaceImport(item.Namespace));
            //}
            //foreach (string @namespace in Namespaces)
            //{
            //    codeNamespace.Imports.Add(new CodeNamespaceImport(@namespace));
            //}
            return codeNamespace;
        }

        private void OverrideProperties(Type currentType, CodeTypeDeclaration wrapProxyClass)
        {
            foreach (PropertyInfo pInfo in FindAllProperties(currentType))
            {
                var getinvocationCode = new CodeSnippetStatement(string.Format("Invocation getinvocation = new Invocation(new object[0], _proxyBaseType.GetProperty(\"{0}\").GetGetMethod(), this);", pInfo.Name));
                var setinvocationCode = new CodeSnippetStatement(string.Format("Invocation setinvocation = new Invocation(new object[]{{value}}, _proxyBaseType.GetProperty(\"{0}\").GetSetMethod(), this);", pInfo.Name));
                var propertyCode = new CodeMemberProperty();
                propertyCode.Name = pInfo.Name;
                propertyCode.Type = GetSimpleType(pInfo.PropertyType);
                propertyCode.Attributes = MemberAttributes.Override | MemberAttributes.Public;

                propertyCode.GetStatements.Add(getinvocationCode);
                propertyCode.SetStatements.Add(setinvocationCode);

                propertyCode.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(getinvocation);"));
                propertyCode.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(setinvocation);"));

                propertyCode.GetStatements.Add(new CodeSnippetStatement(string.Format("var temp_returnData_1024 = base.{0};\r\ngetinvocation.ReturnValue = temp_returnData_1024;", pInfo.Name)));
                propertyCode.SetStatements.Add(new CodeSnippetStatement(string.Format("base.{0} = value;", pInfo.Name)));

                propertyCode.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteAfterCall(getinvocation);"));
                propertyCode.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteAfterCall(setinvocation);"));

                propertyCode.GetStatements.Add(new CodeSnippetStatement("var result = "));
                CodeCastExpression castExpression = new CodeCastExpression(pInfo.PropertyType, new CodeVariableReferenceExpression("getinvocation.ReturnValue"));
                propertyCode.GetStatements.Add(castExpression);
                propertyCode.GetStatements.Add(new CodeSnippetStatement("return result;"));

                wrapProxyClass.Members.Add(propertyCode);
            }
        }

        class UniqueNameHelper
        {
            private HashSet<string> nameSet = new HashSet<string>();

            public string ToUniqueName(string oriname)
            {
                int counter = 0;
                var temp = oriname;
                while (true)
                {
                    if (!nameSet.Contains(temp))
                    {
                        nameSet.Add(temp);
                        return temp;
                    }
                    counter++;
                    temp += counter;
                }
            }
            public void Add(string name)
            {
                nameSet.Add(name);
            }
        }

        private void OverrideMethods(Type currentType, CodeTypeDeclaration wrapProxyClass)
        {
            foreach (MethodInfo methodInfo in FindAllMethods(currentType))
            {
                UniqueNameHelper nameHelper = new UniqueNameHelper();
                var methodCode = new CodeMemberMethod();
                methodCode.Name = methodInfo.Name;
                if (methodInfo.ReturnType != typeof(void))
                    methodCode.ReturnType = GetSimpleType(methodInfo.ReturnType);

                var parameterList = methodInfo.GetParameters();
                foreach (ParameterInfo input in parameterList)
                {
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    nameHelper.Add(input.Name);
                }

                CodeVariableDeclarationStatement v_arguments_Code = new CodeVariableDeclarationStatement("System.Object[]", nameHelper.ToUniqueName("arguments"), new CodeArrayCreateExpression("System.Object", parameterList.Length));
                methodCode.Statements.Add(v_arguments_Code);
                for (int i = 0; i < parameterList.Length; i++)
                {
                    CodeAssignStatement assignCode = new CodeAssignStatement(new CodeVariableReferenceExpression(string.Format("{1}[{0}]", i, v_arguments_Code.Name)), new CodeVariableReferenceExpression(parameterList[i].Name));
                    methodCode.Statements.Add(assignCode);
                }

                CodeMethodInvokeExpression invokeMethodCode;

                var invocation_name = nameHelper.ToUniqueName("invocation");
                CodeSnippetStatement invocationCode = null;
                if (parameterList.Length == 0)
                {
                    invocationCode = new CodeSnippetStatement(string.Format("Invocation {2} = new Invocation({1}, _proxyBaseType.GetMethod(\"{0}\"), this);", methodInfo.Name, v_arguments_Code.Name, invocation_name));
                }
                else
                {
                    var typeArrayName = nameHelper.ToUniqueName("paramTypeList");
                    methodCode.Statements.Add(new CodeSnippetStatement(string.Format("Type[] {0} = new Type[{1}];", typeArrayName, parameterList.Length)));
                    for (int i = 0; i < parameterList.Length; i++)
                    {
                        CodeAssignStatement assignCode = new CodeAssignStatement(new CodeVariableReferenceExpression(string.Format("{1}[{0}]", i, typeArrayName)), new CodeSnippetExpression(string.Format("typeof({0})", parameterList[i].ParameterType)));
                        methodCode.Statements.Add(assignCode);
                    }
                    invocationCode = new CodeSnippetStatement(string.Format("Invocation {2} = new Invocation({1}, _proxyBaseType.GetMethod(\"{0}\", {3}), this);", methodInfo.Name, v_arguments_Code.Name, invocation_name, typeArrayName));
                }

                methodCode.Statements.Add(invocationCode);

                invokeMethodCode = new CodeMethodInvokeExpression();
                invokeMethodCode.Method = new CodeMethodReferenceExpression { MethodName = "ExecuteBeforeCall" };
                invokeMethodCode.Method.TargetObject = new CodeSnippetExpression("_proxyProviderBase");
                invokeMethodCode.Parameters.Add(new CodeVariableReferenceExpression(invocation_name));
                methodCode.Statements.Add(invokeMethodCode);

                methodCode.Attributes = MemberAttributes.Override | MemberAttributes.Public;
                var temp_returnData = nameHelper.ToUniqueName("temp_returnData");
                if (methodInfo.ReturnType != typeof(void))
                {
                    methodCode.Statements.Add(new CodeSnippetStatement(string.Format("var {0} = ", temp_returnData)));
                }
                invokeMethodCode = new CodeMethodInvokeExpression();
                invokeMethodCode.Method = new CodeMethodReferenceExpression { MethodName = "base." + methodInfo.Name };
                for (int i = 0; i < parameterList.Length; i++)
                {
                    invokeMethodCode.Parameters.Add(new CodeCastExpression(parameterList[i].ParameterType, new CodeVariableReferenceExpression(string.Format("{1}[{0}]", i, v_arguments_Code.Name))));
                }
                methodCode.Statements.Add(invokeMethodCode);

                CodeAssignStatement as11 = new CodeAssignStatement(new CodeVariableReferenceExpression(string.Format("{0}.ReturnValue", invocation_name)), new CodeVariableReferenceExpression(temp_returnData));
                methodCode.Statements.Add(as11);

                invokeMethodCode = new CodeMethodInvokeExpression();
                invokeMethodCode.Method = new CodeMethodReferenceExpression { MethodName = "ExecuteAfterCall" };
                invokeMethodCode.Method.TargetObject = new CodeSnippetExpression("_proxyProviderBase");
                invokeMethodCode.Parameters.Add(new CodeVariableReferenceExpression(invocation_name));
                methodCode.Statements.Add(invokeMethodCode);

                if (methodInfo.ReturnType != typeof(void))
                {
                    CodeCastExpression castExpression = new CodeCastExpression(methodInfo.ReturnType, new CodeVariableReferenceExpression(string.Format("{0}.ReturnValue", invocation_name)));
                    methodCode.Statements.Add(new CodeMethodReturnStatement(castExpression));
                }

                wrapProxyClass.Members.Add(methodCode);
            }
        }

        private static CodeTypeReference GetSimpleType(Type t)
        {
            Situation s = SituationHelper.GetSituation(t);
            switch (s)
            {
                case Situation.Dictionary:
                case Situation.SerializableDirtionary:
                    {
                        if (t.IsGenericType)
                        {
                            return new CodeTypeReference(string.Format("{0}<global::{1},global::{2}>"
                                                                       , t.Name.Substring(0, t.Name.Length - 2)
                                                                       , t.GetGenericArguments()[0].FullName
                                                                       , t.GetGenericArguments()[1].FullName
                                                             ));
                        }
                        break;
                    }
                case Situation.IEnumableT:
                case Situation.SerializableIEnumableT:
                    {
                        if (t.IsGenericType)
                        {
                            return
                                new CodeTypeReference(string.Format("{0}<global::{1}>",
                                                                    t.Name.Substring(0, t.Name.Length - 2),
                                                                    t.GetGenericArguments()[0].FullName));
                        }
                        break;
                    }
            }


            return new CodeTypeReference(t);
        }
    }
}