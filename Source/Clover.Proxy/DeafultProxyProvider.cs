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
       

        public DefaultProxyProvider(ProxyConfiguration config):base(config)
        {
           
            
        }

        public override T CreateInstance<T>()
        {
            ProxyAssembly = CreateLocalAssembly<T>(typeof(T).Assembly);

            Type tempType = ProxyAssembly.GetType(TypeInformation.GetLocalProxyClassFullName(typeof(T)));
            return (T)Activator.CreateInstance(tempType,new Object[]
            {
                this
            });
        }

        public Assembly CreateLocalAssembly<T>(Assembly entityAssembly)
        {
            Type currentType = typeof(T);
            string localClassName = TypeInformation.GetLocalProxyClassName(currentType);
            // SituationHelper.GetLocalProxyClassName(CurrentType);

            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetLocalNamespace(currentType));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("Systeym.Collections.Generic"));
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

            var field = new CodeMemberField(typeof(ProxyProviderBase), "_proxyProviderBase");
            wrapProxyClass.Members.Add(field);
            


            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ProxyProviderBase), "_proxyProviderBase"));
            constructor.Statements.Add(new CodeSnippetStatement("this._proxyProviderBase=_proxyProviderBase;"));
            wrapProxyClass.Members.Add(constructor);


            foreach (MethodInfo item in currentType.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual &&
                    !currentType.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {
                    var method = new CodeMemberMethod();
                    method.Name = item.Name;
                    if (item.ReturnType != typeof(void))
                        method.ReturnType = GetSimpleType(item.ReturnType);
                    //method.ReturnType = new CodeTypeReference(item.ReturnType);
                    foreach (ParameterInfo input in item.GetParameters())
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    }

                    
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
                        method.Statements.Add(new CodeSnippetStatement("_proxyProviderBase.AfterCall();"));
                    }

                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));
                    }

                    wrapProxyClass.Members.Add(method);
                }
            }


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