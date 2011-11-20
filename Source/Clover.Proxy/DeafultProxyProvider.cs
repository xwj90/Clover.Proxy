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
        private readonly string DllCachePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly Assembly InterfaceAssembly = typeof(DefaultProxyProvider).Assembly;
        private readonly List<string> Namespaces = new List<string>();
        private ProxyConfiguration config = null;
        private static ConcurrentDictionary<Type, Assembly> assemblies = new ConcurrentDictionary<Type, Assembly>();

        public DefaultProxyProvider(ProxyConfiguration config)
        {
            this.config = config;
            base.BeforeCall = config.BeforeCall;
            base.AfterCall = config.AfterCall;
        }

        public override T CreateInstance<T>()
        {
            var configuratio = ProxyConfiguration.CreateByType(typeof(T));
            if (configuratio.DisableAutoProxy)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            var type = typeof(T);
            var assembly = assemblies.GetOrAdd(type, (t) => { return CreateLocalAssembly<T>(typeof(T).Assembly); });
            Type proxyType = assembly.GetType(TypeInformation.GetLocalProxyClassFullName(typeof(T)));
            return (T)Activator.CreateInstance(proxyType, new Object[] { this });
        }

        private List<MethodInfo> FindAllMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.IsVirtual && !p.IsSpecialName).ToList();
        }
        private List<PropertyInfo> FindAllProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.GetGetMethod().IsVirtual).ToList();
        }

        private Assembly CreateLocalAssembly<T>(Assembly entityAssembly)
        {
            Type currentType = typeof(T);
            string localClassName = TypeInformation.GetLocalProxyClassName(currentType);
            // SituationHelper.GetLocalProxyClassName(currentType);

            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetLocalNamespace(currentType));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            foreach (Type item in InterfaceAssembly.GetTypes())
            {
                sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
            }
            foreach (string @namespace in Namespaces)
            {
                sample.Imports.Add(new CodeNamespaceImport(@namespace));
            }

            //定义一个名为DemoClass的类
            // compunit.ReferencedAssemblies.Add(currentType.Assembly.FullName);

            var wrapProxyClass = new CodeTypeDeclaration(localClassName);
            wrapProxyClass.BaseTypes.Add(currentType);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);

            var baseField = new CodeMemberField(typeof(ProxyProviderBase), "_proxyProviderBase");
            wrapProxyClass.Members.Add(baseField);
            var baseTypeField = new CodeMemberField(typeof(Type), "_proxyBaseType");
            wrapProxyClass.Members.Add(baseTypeField);


            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ProxyProviderBase), "_proxyProviderBase"));
            constructor.Statements.Add(new CodeSnippetStatement("this._proxyProviderBase=_proxyProviderBase;"));
            constructor.Statements.Add(new CodeSnippetStatement(string.Format("this._proxyBaseType = Type.GetType(\"{0}\");", currentType.AssemblyQualifiedName)));
            wrapProxyClass.Members.Add(constructor);

            OverrideMethods(currentType, wrapProxyClass);
            OverrideProperties(currentType, wrapProxyClass);


            var cprovider = new CSharpCodeProvider();
            var fileContent = new StringBuilder();
            using (var sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());
            }

            var cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(currentType.Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (string file in Directory.GetFiles(DllCachePath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }


            cp.OutputAssembly = DllCachePath + currentType.FullName + ".Local.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false; //生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = DllCachePath + @"Class\" + currentType.Namespace + "." + currentType.Name + ".Local.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, fileContent.ToString());

            CompilerResults cr = cprovider.CompileAssemblyFromFile(cp, filePath);

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

                if (this.BeforeCall != null || this.AfterCall != null)
                {
                    propertyCode.GetStatements.Add(getinvocationCode);
                    propertyCode.SetStatements.Add(setinvocationCode);
                }
                if (this.BeforeCall != null)
                {
                    propertyCode.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(getinvocation);"));
                    propertyCode.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(setinvocation);"));
                }

                propertyCode.GetStatements.Add(new CodeSnippetStatement(string.Format("var temp_returnData_1024 = base.{0};\r\ngetinvocation.ReturnValue = temp_returnData_1024;", pInfo.Name)));
                propertyCode.SetStatements.Add(new CodeSnippetStatement(string.Format("base.{0} = value;", pInfo.Name)));

                if (this.AfterCall != null)
                {
                    propertyCode.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(getinvocation);"));
                    propertyCode.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(setinvocation);"));
                }

                propertyCode.GetStatements.Add(new CodeSnippetStatement("var result = "));
                CodeCastExpression castExpression = new CodeCastExpression(pInfo.PropertyType, new CodeVariableReferenceExpression("getinvocation.ReturnValue"));
                propertyCode.GetStatements.Add(castExpression);
                propertyCode.GetStatements.Add(new CodeSnippetStatement("return result;"));

                //propertyCode.GetStatements.Add(new CodeSnippetStatement("return invocation.ReturnValue;"));

                wrapProxyClass.Members.Add(propertyCode);

            }
        }

        private void OverrideMethods(Type currentType, CodeTypeDeclaration wrapProxyClass)
        {
            foreach (MethodInfo methodInfo in FindAllMethods(currentType))
            {
                object[] obj = new object[methodInfo.GetParameters().Length];
                obj[0] = methodInfo.GetParameters()[0].Name;
                var methodCode = new CodeMemberMethod();
                methodCode.Name = methodInfo.Name;
                if (methodInfo.ReturnType != typeof(void))
                    methodCode.ReturnType = GetSimpleType(methodInfo.ReturnType);

                var parameterList = methodInfo.GetParameters();
                foreach (ParameterInfo input in parameterList)
                {
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    // methodCode.Statements.Add(new CodeSnippetExpression(input.Name));
                }

                CodeArrayCreateExpression ca1 = new CodeArrayCreateExpression("System.Object", parameterList.Length);
                CodeVariableDeclarationStatement cv1 = new CodeVariableDeclarationStatement("System.Object[]", "arguments", ca1);
                methodCode.Statements.Add(cv1);
                for (int i = 0; i < parameterList.Length; i++)
                {
                    CodeAssignStatement as1 = new CodeAssignStatement(new CodeVariableReferenceExpression(string.Format("arguments[{0}]", i)), new CodeVariableReferenceExpression(parameterList[i].Name));
                    methodCode.Statements.Add(as1);
                }
                var invocationCode = new CodeSnippetStatement(string.Format("Invocation invocation = new Invocation(arguments, _proxyBaseType.GetMethod(\"{0}\"), this);", methodInfo.Name));
                methodCode.Statements.Add(invocationCode);

                #region ignore log
                bool enableLog = false;
                if (enableLog)
                {
                    string code = "";
                    foreach (ParameterInfo input in methodInfo.GetParameters())
                    {
                        Situation situcation = SituationHelper.GetSituation(input.ParameterType);

                        if (input.ParameterType.IsClass)
                        {
                            code += Environment.NewLine + string.Format("if({0}!=null){{", input.Name);
                            foreach (MemberInfo member in SituationHelper.GetMembers(input.ParameterType))
                            {
                                code += Environment.NewLine +
                                        string.Format("Clover.AgileBet.Logger.Current.WriteEntry({0}.{1});",
                                                      input.Name, member.Name);
                            }
                            code += Environment.NewLine +
                                    string.Format("}}else{{Clover.AgileBet.Logger.Current.WriteEntry({0});}}",
                                                  input.Name);
                        }
                        else
                        {
                            code += Environment.NewLine +
                                    string.Format("Clover.AgileBet.Logger.Current.WriteEntry({0});", input.Name);
                        }
                    }
                    methodCode.Statements.Add(new CodeSnippetStatement(code));
                }
                #endregion

                if (this.BeforeCall != null)
                {
                    methodCode.Statements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(invocation);"));
                }
                methodCode.Attributes = MemberAttributes.Override | MemberAttributes.Public;
                if (methodInfo.ReturnType != typeof(void))
                {
                    methodCode.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                }
                var cs = new CodeMethodInvokeExpression();
                //bug
                cs.Method = new CodeMethodReferenceExpression { MethodName = "base." + methodInfo.Name };
                foreach (ParameterInfo input in methodInfo.GetParameters())
                {
                    Type t = input.ParameterType;
                    Situation situation = SituationHelper.GetSituation(t);
                    cs.Parameters.Add(new CodeSnippetExpression(input.Name));
                }
                methodCode.Statements.Add(cs);

                CodeAssignStatement as11 = new CodeAssignStatement(new CodeVariableReferenceExpression("invocation.ReturnValue"), new CodeVariableReferenceExpression("temp_returnData_1024"));
                methodCode.Statements.Add(as11);

                if (this.AfterCall != null)
                {
                    methodCode.Statements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(invocation);"));
                }

                if (methodInfo.ReturnType != typeof(void))
                {
                    methodCode.Statements.Add(new CodeSnippetStatement("var result = "));
                    CodeCastExpression castExpression = new CodeCastExpression(methodInfo.ReturnType, new CodeVariableReferenceExpression("invocation.ReturnValue"));
                    methodCode.Statements.Add(castExpression);
                    methodCode.Statements.Add(new CodeSnippetStatement("return result;"));
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