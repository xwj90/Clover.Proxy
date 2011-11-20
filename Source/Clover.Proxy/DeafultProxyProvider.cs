using System;
using System.CodeDom;
using System.CodeDom.Compiler;
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
        private Assembly ProxyAssembly;
        private ProxyConfiguration config;

        public DefaultProxyProvider(ProxyConfiguration config)
        {
            this.config = config;
            base.BeforeCall = config.BeforeCall;
            base.AfterCall = config.AfterCall;
        }

        public override T CreateInstance<T>()
        {
            ProxyAssembly = CreateLocalAssembly<T>(typeof(T).Assembly);

            Type tempType = ProxyAssembly.GetType(TypeInformation.GetLocalProxyClassFullName(typeof(T)));
            return (T)Activator.CreateInstance(tempType, new Object[]
            {
                this
            });
        }

        private List<MethodInfo> FindAllMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.IsVirtual && !p.IsSpecialName).ToList();
        }
        private List<PropertyInfo> FindAllProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(p => p.GetGetMethod().IsVirtual).ToList();
        }

        public Assembly CreateLocalAssembly<T>(Assembly entityAssembly)
        {
            Type CurrentType = typeof(T);
            string localClassName = TypeInformation.GetLocalProxyClassName(CurrentType);
            // SituationHelper.GetLocalProxyClassName(CurrentType);

            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetLocalNamespace(CurrentType));
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
            wrapProxyClass.BaseTypes.Add(CurrentType);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);

            var field = new CodeMemberField(typeof(ProxyProviderBase), "_proxyProviderBase");
            wrapProxyClass.Members.Add(field);



            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ProxyProviderBase), "_proxyProviderBase"));
            constructor.Statements.Add(new CodeSnippetStatement("this._proxyProviderBase=_proxyProviderBase;"));
            wrapProxyClass.Members.Add(constructor);

            OverrideMethods(CurrentType, wrapProxyClass);
            OverrideProperties(CurrentType, wrapProxyClass);


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
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(CurrentType.Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (string file in Directory.GetFiles(DllCachePath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }


            cp.OutputAssembly = DllCachePath + CurrentType.FullName + ".Local.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false; //生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = DllCachePath + @"Class\" + CurrentType.Namespace + "." + CurrentType.Name + ".Local.cs";
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

        private void OverrideProperties(Type CurrentType, CodeTypeDeclaration wrapProxyClass)
        {

            foreach (PropertyInfo pInfo in FindAllProperties(CurrentType))
            {
                string str = string.Format("var type = Type.GetType(\"{0}\");", CurrentType.AssemblyQualifiedName);
                str += string.Format("var mi = type.GetProperty(\"{0}\").GetGetMethod();", pInfo.Name);
                var getinvocationCode = new CodeSnippetStatement(str + "Invocation getinvocation = new Invocation(new object[0], mi, this);");
                var setinvocationCode = new CodeSnippetStatement(str + "Invocation setinvocation = new Invocation(new object[]{value}, mi, this);");
                var property = new CodeMemberProperty();
                property.Name = pInfo.Name;
                property.Type = GetSimpleType(pInfo.PropertyType);
                property.Attributes = MemberAttributes.Override | MemberAttributes.Public;

                if (this.BeforeCall != null)
                {
                    property.GetStatements.Add(getinvocationCode);
                    property.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(getinvocation);"));
                    property.SetStatements.Add(setinvocationCode);
                    property.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(setinvocation);"));
                }

                property.GetStatements.Add(new CodeSnippetStatement(string.Format("var temp_returnData_1024=base.{0};", pInfo.Name)));
                property.SetStatements.Add(new CodeSnippetStatement(string.Format("base.{0} = value;", pInfo.Name)));

                if (this.AfterCall != null)
                {
                    property.GetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(getinvocation);"));
                    property.SetStatements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(setinvocation);"));
                }

                property.GetStatements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));

                wrapProxyClass.Members.Add(property);

            }
        }

        private void OverrideMethods(Type CurrentType, CodeTypeDeclaration wrapProxyClass)
        {
            foreach (MethodInfo item in FindAllMethods(CurrentType))
            {
                var method = new CodeMemberMethod();
                method.Name = item.Name;
                if (item.ReturnType != typeof(void))
                    method.ReturnType = GetSimpleType(item.ReturnType);

                foreach (ParameterInfo input in item.GetParameters())
                {
                    method.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                }

                #region ignore log
                bool enableLog = false;
                if (enableLog)
                {
                    string code = "";
                    foreach (ParameterInfo input in item.GetParameters())
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
                    method.Statements.Add(new CodeSnippetStatement(code));
                }
                #endregion

                if (this.BeforeCall != null)
                {
                    method.Statements.Add(new CodeSnippetStatement("_proxyProviderBase.ExecuteBeforeCall(null);"));
                }
                method.Attributes = MemberAttributes.Override | MemberAttributes.Public;
                if (item.ReturnType != typeof(void))
                {
                    method.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                }
                var cs = new CodeMethodInvokeExpression();
                //bug
                cs.Method = new CodeMethodReferenceExpression { MethodName = "base." + item.Name };
                foreach (ParameterInfo input in item.GetParameters())
                {
                    Type t = input.ParameterType;
                    Situation situation = SituationHelper.GetSituation(t);
                    cs.Parameters.Add(new CodeSnippetExpression(input.Name));
                }
                method.Statements.Add(cs);

                if (this.AfterCall != null)
                {
                    method.Statements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall(null);"));
                }

                if (item.ReturnType != typeof(void))
                {
                    method.Statements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));
                }

                wrapProxyClass.Members.Add(method);

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