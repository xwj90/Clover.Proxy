
using System;
using System.Reflection;
using System.Collections.Generic;
using System.CodeDom;
using System.Linq;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Configuration;

namespace Clover.Proxy
{

    public class AssemblyHelper<T>
    {
        public static string DllCachePath = ConfigurationManager.AppSettings["Clover.DllCachePath"];

        public static bool EnableLogInputParameter = Convert.ToBoolean(ConfigurationManager.AppSettings["Clover.EnableLogInputParametersPassToInternalComponents"]);

        public static bool EnableLogReturnValue = Convert.ToBoolean(ConfigurationManager.AppSettings["Clover.EnableLogReturnValueFromInternalComponents"]);


        private static Type CurrentType = typeof(T);
        private static HashSet<Type> EntityTypes = new HashSet<Type>();
        private static HashSet<string> Namespaces = new HashSet<string>();
        private static Assembly InterfaceAssembly = typeof(IWrapper).Assembly;

        static AssemblyHelper()
        {
            if (string.IsNullOrEmpty(DllCachePath))
            {
                DllCachePath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }


        public static Assembly CreateEntityAssembly()
        {

            CodeCompileUnit compunit = new CodeCompileUnit();
            CodeNamespace sample = new CodeNamespace(TypeInformation.GetEntityNamespace(CurrentType));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            sample.Imports.Add(new CodeNamespaceImport(CurrentType.Namespace));
            foreach (var item in InterfaceAssembly.GetTypes())
            {
                sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
            }

            foreach (var @namespace in Namespaces)
            {
                sample.Imports.Add(new CodeNamespaceImport(@namespace));
            }


            foreach (var item in CurrentType.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual && !CurrentType.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {

                    foreach (var input in item.GetParameters())
                    {
                        DelareSerializableParameter(input.ParameterType, sample);
                    }
                    if (item.ReturnType != typeof(void))
                    {

                        DelareSerializableParameter(item.ReturnType, sample);
                    }
                }

            }
            if (EntityTypes.Count == 0)
                return null;

            CSharpCodeProvider cprovider = new CSharpCodeProvider();

            StringBuilder fileContent = new StringBuilder();
            using (StringWriter sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());

            }

            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(CurrentType.Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (var file in Directory.GetFiles(DllCachePath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }



            cp.OutputAssembly = DllCachePath + CurrentType.FullName + ".Entity.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false;//生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = DllCachePath + @"Class\" + CurrentType.FullName + ".Entity.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, fileContent.ToString());

            CompilerResults cr = cprovider.CompileAssemblyFromFile(cp, filePath);

            String outputMessage = "";
            foreach (var item in cr.Output)
            {
                outputMessage += item + Environment.NewLine;
            }
            if (cr.Errors.Count > 0)
            {
                throw new Exception("complie eneity proxy class error:" + Environment.NewLine + outputMessage);
            }
            return cr.CompiledAssembly;
        }

        private static void DelareSerializableParameter(Type parameterType, CodeNamespace sample)
        {

            if (EntityTypes.Contains(parameterType))
                return;
            foreach (var item in parameterType.GetGenericArguments())
            {
                //Namespaces.Add(item.Namespace);
                foreach (var newType in SituationHelper.GetToBeSerializableTypes(item))
                {
                    DelareSerializableParameter(newType, sample);
                }
            }
            foreach (var item in SituationHelper.GetMembers(parameterType))
            {

                Type type = SituationHelper.GetRealType(item);
                //Namespaces.Add(type.Namespace);
                foreach (var newType in SituationHelper.GetToBeSerializableTypes(type))
                {
                    DelareSerializableParameter(newType, sample);
                }
            }
            Situation currentSitucation = SituationHelper.GetSituation(parameterType);

            //Namespaces.Add(parameterType.Namespace);
            if (currentSitucation != Situation.UnSerializable)
                return;



            if (parameterType == typeof(System.Object))
                return;

            sample.Imports.Add(new CodeNamespaceImport(parameterType.Namespace));
            List<CodeTypeDeclaration> list = new List<CodeTypeDeclaration>();
            string className = TypeInformation.GetEntityProxyClassName(parameterType);// SituationHelper.GetSerializableClassName(parameterType);
            CodeTypeDeclaration newEntity = new CodeTypeDeclaration(className);
            newEntity.BaseTypes.Add(parameterType);
            newEntity.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(newEntity);
            newEntity.BaseTypes.Add(typeof(ISerializable));


            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "GetObjectData";
            method.Attributes = MemberAttributes.Public;
            method.ReturnType = null;
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SerializationInfo), "info"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(StreamingContext), "context"));

            string code = "";
            foreach (var item in SituationHelper.GetMembers(parameterType))
            {
                Type type = SituationHelper.GetRealType(item);
                Situation situcation = SituationHelper.GetSituation(type);
                switch (situcation)
                {
                    case Situation.SerializableNullableT:
                    case Situation.Serializable:
                    case Situation.SerializableArray:
                    case Situation.SerializableIEnumableT:
                    case Situation.SerializableDirtionary:
                        {
                            code += Environment.NewLine + string.Format(" info.AddValue(\"{0}\", {0});", item.Name);
                            break;
                        }
                    case Situation.UnSerializable:
                        {
                            code += Environment.NewLine + string.Format(" info.AddValue(\"{0}\",  {0}==null ? null : new Serializable_{1}({0}));", item.Name, type.Name);
                            break;
                        }
                    case Situation.Array:
                        {
                            code += Environment.NewLine + string.Format(" info.AddValue(\"{0}\",  {0}==null ? null : {0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)).ToArray()  );", item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }
                    case Situation.IEnumableT:
                        {
                            code += Environment.NewLine + string.Format(" info.AddValue(\"{0}\",  {0}==null ? null : {0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)) );", item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }
                    case Situation.Dictionary:
                        {
                            code += Environment.NewLine + string.Format(" info.AddValue(\"{0}\",  {1} );", item.Name, SituationHelper.GetExpression(type, item.Name));
                            break;
                        }
                }
            }
            method.Statements.Add(new CodeSnippetStatement(code));
            newEntity.Members.Add(method);

            // type parse
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(parameterType, "value"));
            code = "";
            foreach (var item in SituationHelper.GetMembers(parameterType))
            {
                Type type = SituationHelper.GetRealType(item);
                Situation situcation = SituationHelper.GetSituation(type);

                switch (situcation)
                {
                    case Situation.SerializableArray:
                    case Situation.SerializableNullableT:
                    case Situation.Serializable:
                    case Situation.SerializableIEnumableT:
                    case Situation.SerializableDirtionary:
                        {
                            code += Environment.NewLine + string.Format(" this.{0}=value.{0};", item.Name);
                            break;
                        }
                    case Situation.UnSerializable:
                        {
                            code += Environment.NewLine + string.Format(@"
if(value.{0}!=null)
this.{0}= new Serializable_{1}(value.{0});", item.Name, type.Name);
                            break;
                        }
                    case Situation.Array:
                        {
                            code += Environment.NewLine + string.Format(@"
if(value.{0}!=null)
this.{0}= value.{0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)).ToArray();", item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.IEnumableT:
                        {
                            code += Environment.NewLine + string.Format(@"
if(value.{0}!=null)
this.{0}= value.{0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)); ", item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.Dictionary:
                        {
                            code += Environment.NewLine + string.Format(@"
if(value.{0}!=null)
this.{0}= {1}; ", item.Name, SituationHelper.GetExpression(type, "value." + item.Name));
                            break;
                        }
                }


            }
            constructor.Statements.Add(new CodeSnippetStatement(code));
            newEntity.Members.Add(constructor);



            constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SerializationInfo), "info"));
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(StreamingContext), "context"));


            //constructor
            code = "";
            foreach (var item in SituationHelper.GetMembers(parameterType))
            {

                Type type = SituationHelper.GetRealType(item);
                Situation situcation = SituationHelper.GetSituation(type);

                switch (situcation)
                {

                    case Situation.Serializable:
                        {
                            code += Environment.NewLine + string.Format(" this.{0}=info.{1}(\"{0}\");", item.Name, SituationHelper.GetMethodName(type));
                            break;
                        }
                    case Situation.Array:
                    case Situation.SerializableArray:
                        {
                            code += Environment.NewLine + string.Format(" this.{0}=({1})info.GetValue(\"{0}\",typeof({1}));", item.Name, SituationHelper.GetMethodName(type, false));
                            break;
                        }
                    case Situation.UnSerializable:
                        {
                            code += Environment.NewLine + string.Format(" this.{0}=({1})info.GetValue(\"{0}\",typeof({1}));", item.Name, SituationHelper.GetMethodName(type, false));
                            break;
                        }
                    case Situation.SerializableNullableT:
                        {

                            code += Environment.NewLine + string.Format(" if (info.GetValue(\"{0}\",typeof(object))!=null) this.{0}=info.{1}(\"{0}\");", item.Name, SituationHelper.GetMethodName(type.GetGenericArguments()[0]));
                            break;
                        }
                    case Situation.IEnumableT:
                    case Situation.SerializableIEnumableT:
                        {
                            string enumableType = type.GetGenericTypeDefinition().Name.Substring(0, type.GetGenericTypeDefinition().Name.Length - 2);

                            code += Environment.NewLine + string.Format(" this.{0}=({1}<{2}>)info.GetValue(\"{0}\",typeof({1}<{2}>));", item.Name, enumableType, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.Dictionary:
                    case Situation.SerializableDirtionary:
                        {
                            string enumableType = type.GetGenericTypeDefinition().Name.Substring(0, type.GetGenericTypeDefinition().Name.Length - 2);

                            code += Environment.NewLine + string.Format(" this.{0}=({1}<{2}>)info.GetValue(\"{0}\",typeof({1}<{2},{3}>));", item.Name, enumableType, SituationHelper.GetInternalTypeFormArray(type)[0].Name, SituationHelper.GetInternalTypeFormArray(type)[1].Name);
                            break;
                        }
                }

            }
            constructor.Statements.Add(new CodeSnippetStatement(code));
            newEntity.Members.Add(constructor);

            EntityTypes.Add(parameterType);
            Namespaces.Add(parameterType.Namespace);

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
                            return new CodeTypeReference(string.Format("{0}<global::{1}>", t.Name.Substring(0, t.Name.Length - 2), t.GetGenericArguments()[0].FullName));
                        }
                        break;
                    }
            }


            return new CodeTypeReference(t);
        }
        public static Assembly CreateLocalAssembly(Assembly entityAssembly)
        {

            string localClassName = TypeInformation.GetLocalProxyClassName(CurrentType);// SituationHelper.GetLocalProxyClassName(CurrentType);

            CodeCompileUnit compunit = new CodeCompileUnit();
            CodeNamespace sample = new CodeNamespace(TypeInformation.GetLocalNamespace(CurrentType));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            foreach (var item in InterfaceAssembly.GetTypes())
            {
                sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
            }
            foreach (var @namespace in Namespaces)
            {
                sample.Imports.Add(new CodeNamespaceImport(@namespace));
            }

            //定义一个名为DemoClass的类
            // compunit.ReferencedAssemblies.Add(currentType.Assembly.FullName);

            CodeTypeDeclaration wrapProxyClass = new CodeTypeDeclaration(localClassName);
            wrapProxyClass.BaseTypes.Add(CurrentType);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);

            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            wrapProxyClass.Members.Add(constructor);


            foreach (var item in CurrentType.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual && !CurrentType.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {
                    System.CodeDom.CodeMemberMethod method = new CodeMemberMethod();
                    method.Name = item.Name;
                    if (item.ReturnType != typeof(void))
                        method.ReturnType = GetSimpleType(item.ReturnType);
                    //method.ReturnType = new CodeTypeReference(item.ReturnType);
                    foreach (var input in item.GetParameters())
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    }

                    bool enableLog = Convert.ToBoolean(ConfigurationManager.AppSettings["AgileBetSdk.EnableLogInputParametersPassToInternalComponents"]);
                    if (enableLog)
                    {
                        string code = "";
                        foreach (var input in item.GetParameters())
                        {
                            Situation situcation = SituationHelper.GetSituation(input.ParameterType);

                            if (input.ParameterType.IsClass)
                            {
                                code += Environment.NewLine + string.Format("if({0}!=null){{", input.Name);
                                foreach (var member in SituationHelper.GetMembers(input.ParameterType))
                                {
                                    code += Environment.NewLine + string.Format("Clover.AgileBet.Logger.Current.WriteEntry({0}.{1});", input.Name, member.Name);
                                }
                                code += Environment.NewLine + string.Format("}}else{{Clover.AgileBet.Logger.Current.WriteEntry({0});}}", input.Name);

                            }
                            else
                            {

                                code += Environment.NewLine + string.Format("Clover.AgileBet.Logger.Current.WriteEntry({0});", input.Name);
                            }

                        }
                        method.Statements.Add(new CodeSnippetStatement(code));
                    }
                    method.Attributes = MemberAttributes.Override | MemberAttributes.Public;
                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                    }
                    CodeMethodInvokeExpression cs = new CodeMethodInvokeExpression();
                    cs.Method = new CodeMethodReferenceExpression() { MethodName = "BaseWrapper<" + CurrentType.Name + ">.RemoteT." + item.Name };
                    foreach (var input in item.GetParameters())
                    {
                        Type t = input.ParameterType;
                        Situation situation = SituationHelper.GetSituation(t);
                        cs.Parameters.Add(new CodeSnippetExpression(input.Name));
                        // cs.Parameters.Add(new CodeSnippetExpression(SituationHelper.GetExpression(t, input.Name)));
                    }
                    method.Statements.Add(cs);

                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));
                    }

                    wrapProxyClass.Members.Add(method);
                }
            }


            CSharpCodeProvider cprovider = new CSharpCodeProvider();
            StringBuilder fileContent = new StringBuilder();
            using (StringWriter sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());

            }

            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(CurrentType.Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (var file in Directory.GetFiles(DllCachePath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }


            cp.OutputAssembly = DllCachePath + CurrentType.FullName + ".Local.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false;//生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = DllCachePath + @"Class\" + CurrentType.Namespace + "." + CurrentType.Name + ".Local.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, fileContent.ToString());

            CompilerResults cr = cprovider.CompileAssemblyFromFile(cp, filePath);

            String outputMessage = "";
            foreach (var item in cr.Output)
            {
                outputMessage += item + Environment.NewLine;
            }
            if (cr.Errors.Count > 0)
            {
                throw new Exception("complie local proxy class error:" + Environment.NewLine + outputMessage);
            }
            return cr.CompiledAssembly;

        }


        internal static Assembly CreateRemoteAssembly(Assembly entityAssembly)
        {
            string remoteProxyClassName = TypeInformation.GetRemoteProxyClassName(CurrentType);

            CodeCompileUnit compunit = new CodeCompileUnit();
            CodeNamespace sample = new CodeNamespace(TypeInformation.GetRemoteNamespace(CurrentType));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport("System.Security.Principal"));
            sample.Imports.Add(new CodeNamespaceImport("System.Threading"));


            sample.Imports.Add(new CodeNamespaceImport(typeof(ServiceContext).Namespace));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            foreach (var item in InterfaceAssembly.GetTypes())
            {
                sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
            }
            foreach (var @namespace in Namespaces)
            {
                sample.Imports.Add(new CodeNamespaceImport(@namespace));
            }

            CodeTypeDeclaration wrapProxyClass = new CodeTypeDeclaration(remoteProxyClassName);
            wrapProxyClass.BaseTypes.Add(CurrentType);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);

            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            wrapProxyClass.Members.Add(constructor);

            foreach (var item in CurrentType.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual && !CurrentType.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {
                    System.CodeDom.CodeMemberMethod method = new CodeMemberMethod();
                    method.Name = item.Name;
                    // method.Statements.Add(
                    if (item.ReturnType != typeof(void))
                        method.ReturnType = GetSimpleType(item.ReturnType);
                    // method.ReturnType = new CodeTypeReference(item.ReturnType);
                    foreach (var input in item.GetParameters())
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    }
                    //write line
                    // method.Statements.Add(new CodeSnippetStatement(typeof(ServiceContext).Namespace + "." + typeof(ServiceContext).Name + ".SetThreadPrincipal();"));
                    method.Statements.Add(new CodeSnippetStatement(
@"
EventMonitor.BeforeCall(null);
WindowsIdentity WindowsIdentity_1024 = WindowsIdentity.GetCurrent();
try 
{
"));

                    method.Attributes = MemberAttributes.Override | MemberAttributes.Public;

                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                    }
                    CodeMethodInvokeExpression cs = new CodeMethodInvokeExpression();
                    cs.Method = new CodeMethodReferenceExpression() { MethodName = "base." + item.Name };
                    foreach (var input in item.GetParameters())
                    {
                        cs.Parameters.Add(new CodeSnippetExpression(input.Name));
                    }
                    method.Statements.Add(cs);

                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));
                        //                        Type type = item.ReturnType;
                        //                        Situation situation = SituationHelper.GetSituation(item.ReturnType);

                        //                        switch (situation)
                        //                        {
                        //                            case Situation.SerializableNullableT:
                        //                            case Situation.SerializableArray:
                        //                            case Situation.SerializableDirtionary:
                        //                            case Situation.Serializable:
                        //                                {
                        //                                    method.Statements.Add(new CodeSnippetStatement("return temp_returnData_1024;"));
                        //                                    break;
                        //                                }

                        //                            case Situation.UnSerializable:
                        //                                {
                        //                                    method.Statements.Add(new CodeSnippetStatement("return new Serializable_" + type.Name + "(temp_returnData_1024);"));

                        //                                    break;
                        //                                }
                        //                            case Situation.Array:
                        //                                {
                        //                                    method.Statements.Add(new CodeSnippetStatement(string.Format(@"
                        //if(temp_returnData_1024==null) return null;
                        //return temp_returnData_1024.ToList().ConvertAll<{0}>(p => p==null ? null: new Serializable_{0}(p)).ToArray();", SituationHelper.GetInternalTypeFormArray(type)[0].Name)));
                        //                                    break;
                        //                                }
                        //                            case Situation.IEnumableT:
                        //                                {
                        //                                    method.Statements.Add(new CodeSnippetStatement(string.Format(@"
                        //if(temp_returnData_1024==null) return null;
                        //return temp_returnData_1024.ToList().ConvertAll<{0}>(p => p==null ? null: new Serializable_{0}(p));", SituationHelper.GetInternalTypeFormArray(type)[0].Name)));
                        //                                    break;
                        //                                }
                        //                            case Situation.Dictionary:
                        //                                {
                        //                                    Type[] types = SituationHelper.GetInternalTypeFormArray(type);
                        //                                    method.Statements.Add(new CodeSnippetStatement(string.Format(@"
                        //if(temp_returnData_1024==null) return null;
                        //return {0} ;", SituationHelper.GetExpression(type, "temp_returnData_1024"))));
                        //                                    break;
                        //                                }

                        //                        }


                    }
                    method.Statements.Add(new CodeSnippetStatement(
@"
}
catch (Exception ex_1024)
                    {
                        Logger.Current.WriteEntry(ex_1024);
                        if (!(ex_1024.GetType().IsSerializable))
                        {
                           
                            throw ErrorService.CreateException<InvalidOperationException>(ErrorCode.InternalCompontentException, ex_1024.GetType().Name, ex_1024.Message, ex_1024.StackTrace);
                        }
                        throw;
                    }
finally
{
if(WindowsIdentity_1024!=null)
WindowsIdentity_1024.Impersonate();

EventMonitor.AfterCall();
}
"));


                    wrapProxyClass.Members.Add(method);
                }
            }


            CSharpCodeProvider cprovider = new CSharpCodeProvider();

            StringBuilder fileContent = new StringBuilder();
            using (StringWriter sw = new StringWriter(fileContent))
            {

                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());

            }

            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(CurrentType.Assembly.Location));
            cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (var file in Directory.GetFiles(DllCachePath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }

            cp.OutputAssembly = DllCachePath + CurrentType.FullName + ".Remote.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false;
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = DllCachePath + @"Class\" + CurrentType.Namespace + "." + CurrentType.Name + ".Remote.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            File.WriteAllText(filePath, fileContent.ToString());

            CompilerResults cr = cprovider.CompileAssemblyFromFile(cp, filePath);


            String outputMessage = "";
            foreach (var item in cr.Output)
            {
                outputMessage += item + Environment.NewLine;
            }

            if (cr.Errors.Count > 0)
            {
                throw new Exception("complie remote proxy class error:" + Environment.NewLine + outputMessage);
            }
            return cr.CompiledAssembly;

        }


        private static void RefComponents(CompilerParameters cp, IEnumerable<Type> types)
        {
            foreach (var type in EntityTypes)
            {
                if (!type.Assembly.GlobalAssemblyCache)
                    cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(type.Assembly.Location));

                foreach (var @assemblyName in type.Assembly.GetReferencedAssemblies())
                {
                    string filePath = DllCachePath + @assemblyName.Name + ".Dll";
                    if (File.Exists(filePath))
                    {
                        cp.ReferencedAssemblies.Add(filePath);
                    }
                }
            }


        }
    }

}
