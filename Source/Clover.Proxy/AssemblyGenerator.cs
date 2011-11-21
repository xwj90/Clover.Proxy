using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Generic;
using Clover.Proxy.OldDesign;
using System.CodeDom;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CSharp;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
namespace Clover.Proxy
{
    internal class AssemblyGenerator : ProxyProviderBase
    {

        internal static Assembly CreateEntityAssembly(Type type, ProxyConfiguration config, params Assembly[] dependAssemblies)
        {
            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetEntityNamespace(type));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            sample.Imports.Add(new CodeNamespaceImport(type.Namespace));
            foreach (var assembly in dependAssemblies)
            {
                foreach (Type item in assembly.GetTypes())
                {
                    sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
                }
            }
            List<Type> entityTypes = new List<Type>();
            foreach (MethodInfo item in type.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual &&
                    !type.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {
                    foreach (ParameterInfo input in item.GetParameters())
                    {
                        DelareSerializableParameter(entityTypes, input.ParameterType, sample);
                    }
                    if (item.ReturnType != typeof(void))
                    {
                        DelareSerializableParameter(entityTypes, item.ReturnType, sample);
                    }
                }
            }
            if (entityTypes.Count == 0)
                return null;

            var cprovider = new CSharpCodeProvider();

            var fileContent = new StringBuilder();
            using (var sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());
            }

            var cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(type.Assembly.Location));
            cp.OutputAssembly = config.DllCachedPath + type.FullName + ".Entity.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false; //生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            CompilerResults cr = null;

            string filePath = config.DllCachedPath + @"Class\" + type.FullName + ".Entity.cs";
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, fileContent.ToString());

            cr = cprovider.CompileAssemblyFromFile(cp, filePath);

            String outputMessage = "";
            foreach (string item in cr.Output)
            {
                outputMessage += item + Environment.NewLine;
            }
            if (cr.Errors.Count > 0)
            {
                throw new Exception("complie eneity proxy class error:" + Environment.NewLine + outputMessage);
            }
            //AppDomain.CurrentDomain.Load(cr.CompiledAssembly);
            return cr.CompiledAssembly;
        }

        internal static Assembly CreateLocalAssembly(Type type, ProxyConfiguration config, params Assembly[] dependAssemblies)
        {
            string localClassName = TypeInformation.GetLocalProxyClassName(type);
            // SituationHelper.GetLocalProxyClassName(CurrentType);

            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetLocalNamespace(type));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport(typeof(RemoteRunner<>).Namespace));
            foreach (var assembly in dependAssemblies)
            {
                foreach (Type item in assembly.GetTypes())
                {
                    sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
                }
            }

            //定义一个名为DemoClass的类
            // compunit.ReferencedAssemblies.Add(currentType.Assembly.FullName);

            var wrapProxyClass = new CodeTypeDeclaration(localClassName);
            wrapProxyClass.BaseTypes.Add(type);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);


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



            foreach (MethodInfo item in type.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual &&
                    !type.BaseType.GetMethods().Any(p => p.Name == item.Name))
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


                    method.Attributes = MemberAttributes.Override | MemberAttributes.Public;
                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                    }
                    var cs = new CodeMethodInvokeExpression();
                    cs.Method = new CodeMethodReferenceExpression { MethodName = "RemoteRunner<" + type.Name + ">.RemoteT." + item.Name };
                    foreach (ParameterInfo input in item.GetParameters())
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


            var cprovider = new CSharpCodeProvider();
            var fileContent = new StringBuilder();
            using (var sw = new StringWriter(fileContent))
            {
                cprovider.GenerateCodeFromCompileUnit(compunit, sw, new CodeGeneratorOptions());
            }

            var cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(typeof(RemoteRunner<>).Assembly.Location));
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(type.Assembly.Location));
            //cp.ReferencedAssemblies.Add(DllCachePath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (string file in Directory.GetFiles(config.DllCachedPath, "*.dll"))
            {
                if (file.ToUpper().StartsWith("Clover."))
                    continue;
                cp.ReferencedAssemblies.Add(file);
            }


            cp.OutputAssembly = config.DllCachedPath + type.FullName + ".Local.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false; //生成EXE,不是DLL 
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = config.DllCachedPath + @"Class\" + type.Namespace + "." + type.Name + ".Local.cs";
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

        internal static Assembly CreateRemoteAssembly(Type type, ProxyConfiguration config, params Assembly[] dependAssemblies)
        {
            string remoteProxyClassName = TypeInformation.GetRemoteProxyClassName(type);

            var compunit = new CodeCompileUnit();
            var sample = new CodeNamespace(TypeInformation.GetRemoteNamespace(type));
            compunit.Namespaces.Add(sample);

            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Linq"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport("System.Security.Principal"));
            sample.Imports.Add(new CodeNamespaceImport("System.Threading"));


            sample.Imports.Add(new CodeNamespaceImport(typeof(ServiceContext).Namespace));
            sample.Imports.Add(new CodeNamespaceImport(typeof(BaseWrapper<>).Namespace));
            foreach (var assembly in dependAssemblies)
            {
                foreach (Type item in assembly.GetTypes())
                {
                    sample.Imports.Add(new CodeNamespaceImport(item.Namespace));
                }
            }

            var wrapProxyClass = new CodeTypeDeclaration(remoteProxyClassName);
            wrapProxyClass.BaseTypes.Add(type);
            wrapProxyClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(wrapProxyClass);

            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            wrapProxyClass.Members.Add(constructor);

            foreach (MethodInfo item in type.GetMethods())
            {
                if (item.IsPublic && !item.IsStatic && item.IsVirtual &&
                    !type.BaseType.GetMethods().Any(p => p.Name == item.Name))
                {
                    var method = new CodeMemberMethod();
                    method.Name = item.Name;
                    // method.Statements.Add(
                    if (item.ReturnType != typeof(void))
                        method.ReturnType = GetSimpleType(item.ReturnType);
                    // method.ReturnType = new CodeTypeReference(item.ReturnType);
                    foreach (ParameterInfo input in item.GetParameters())
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(input.ParameterType, input.Name));
                    }
                    //write line
                    // method.Statements.Add(new CodeSnippetStatement(typeof(ServiceContext).Namespace + "." + typeof(ServiceContext).Name + ".SetThreadPrincipal();"));

                    if (true) //if enable before call
                    {
                        // CodeMemberMethod beforeCallMethod = new CodeMemberMethod();
                        //beforeCallMethod.Parameters=new CodeParameterDeclarationExpression(typeof(object[]),
                       // method.Statements.Add(new CodeSnippetStatement("EventMonitor.BeforeCall(null);"));
                    }
                    method.Statements.Add(new CodeSnippetStatement(
                                              @"
WindowsIdentity WindowsIdentity_1024 = WindowsIdentity.GetCurrent();
try 
{
"));

                    method.Attributes = MemberAttributes.Override | MemberAttributes.Public;

                    if (item.ReturnType != typeof(void))
                    {
                        method.Statements.Add(new CodeSnippetStatement("var temp_returnData_1024="));
                    }
                    var cs = new CodeMethodInvokeExpression();
                    cs.Method = new CodeMethodReferenceExpression { MethodName = "base." + item.Name };
                    foreach (ParameterInfo input in item.GetParameters())
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

//EventMonitor.AfterCall();
}
"));


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
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(typeof(ServiceContext).Assembly.Location));
            cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(type.Assembly.Location));
            //cp.ReferencedAssemblies.Add(config.DllCachedPath + Path.GetFileName(InterfaceAssembly.Location));
            //RefComponents(cp, EntityTypes);
            foreach (var assembly in dependAssemblies)
            {
                cp.ReferencedAssemblies.Add(assembly.Location);
            }

            //foreach (string file in Directory.GetFiles(config.DllCachedPath, "*.dll"))
            //{
            //    if (file.ToUpper().StartsWith("Clover."))
            //        continue;
            //    cp.ReferencedAssemblies.Add(file);
            //}

            cp.OutputAssembly = config.DllCachedPath + type.FullName + ".Remote.dll";
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false;
            cp.WarningLevel = 4;
            cp.TreatWarningsAsErrors = false;

            string filePath = config.DllCachedPath + @"Class\" + type.Namespace + "." + type.Name + ".Remote.cs";
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
                throw new Exception("complie remote proxy class error:" + Environment.NewLine + outputMessage);
            }
            return cr.CompiledAssembly;
        }

        private static void DelareSerializableParameter(List<Type> entityTypes, Type parameterType, CodeNamespace sample)
        {
            if (entityTypes.Contains(parameterType))
                return;
            else
                entityTypes.Add(parameterType);
            foreach (Type item in parameterType.GetGenericArguments())
            {
                //Namespaces.Add(item.Namespace);
                foreach (Type newType in SituationHelper.GetToBeSerializableTypes(item))
                {
                    DelareSerializableParameter(entityTypes, newType, sample);
                }
            }
            foreach (MemberInfo item in SituationHelper.GetMembers(parameterType))
            {
                Type type = SituationHelper.GetRealType(item);
                //Namespaces.Add(type.Namespace);
                foreach (Type newType in SituationHelper.GetToBeSerializableTypes(type))
                {
                    DelareSerializableParameter(entityTypes, newType, sample);
                }
            }
            Situation currentSitucation = SituationHelper.GetSituation(parameterType);

            //Namespaces.Add(parameterType.Namespace);
            if (currentSitucation != Situation.UnSerializable)
                return;


            if (parameterType == typeof(Object))
                return;

            sample.Imports.Add(new CodeNamespaceImport(parameterType.Namespace));
            var list = new List<CodeTypeDeclaration>();
            string className = TypeInformation.GetEntityProxyClassName(parameterType);
            // SituationHelper.GetSerializableClassName(parameterType);
            var newEntity = new CodeTypeDeclaration(className);
            newEntity.BaseTypes.Add(parameterType);
            newEntity.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            sample.Types.Add(newEntity);
            newEntity.BaseTypes.Add(typeof(ISerializable));


            var method = new CodeMemberMethod();
            method.Name = "GetObjectData";
            method.Attributes = MemberAttributes.Public;
            method.ReturnType = null;
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SerializationInfo), "info"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(StreamingContext), "context"));

            string code = "";
            foreach (MemberInfo item in SituationHelper.GetMembers(parameterType))
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
                            code += Environment.NewLine +
                                    string.Format(
                                        " info.AddValue(\"{0}\",  {0}==null ? null : new Serializable_{1}({0}));",
                                        item.Name, type.Name);
                            break;
                        }
                    case Situation.Array:
                        {
                            code += Environment.NewLine +
                                    string.Format(
                                        " info.AddValue(\"{0}\",  {0}==null ? null : {0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)).ToArray()  );",
                                        item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }
                    case Situation.IEnumableT:
                        {
                            code += Environment.NewLine +
                                    string.Format(
                                        " info.AddValue(\"{0}\",  {0}==null ? null : {0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)) );",
                                        item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }
                    case Situation.Dictionary:
                        {
                            code += Environment.NewLine +
                                    string.Format(" info.AddValue(\"{0}\",  {1} );", item.Name,
                                                  SituationHelper.GetExpression(type, item.Name));
                            break;
                        }
                }
            }
            method.Statements.Add(new CodeSnippetStatement(code));
            newEntity.Members.Add(method);

            // type parse
            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(parameterType, "value"));
            code = "";
            foreach (MemberInfo item in SituationHelper.GetMembers(parameterType))
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
                            code += Environment.NewLine +
                                    string.Format(@"
if(value.{0}!=null)
this.{0}= new Serializable_{1}(value.{0});",
                                                  item.Name, type.Name);
                            break;
                        }
                    case Situation.Array:
                        {
                            code += Environment.NewLine +
                                    string.Format(
                                        @"
if(value.{0}!=null)
this.{0}= value.{0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)).ToArray();",
                                        item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.IEnumableT:
                        {
                            code += Environment.NewLine +
                                    string.Format(
                                        @"
if(value.{0}!=null)
this.{0}= value.{0}.ToList().ConvertAll<{1}>(p => p==null ? null:  new Serializable_{1}(p)); ",
                                        item.Name, SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.Dictionary:
                        {
                            code += Environment.NewLine +
                                    string.Format(@"
if(value.{0}!=null)
this.{0}= {1}; ", item.Name,
                                                  SituationHelper.GetExpression(type, "value." + item.Name));
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
            foreach (MemberInfo item in SituationHelper.GetMembers(parameterType))
            {
                Type type = SituationHelper.GetRealType(item);
                Situation situcation = SituationHelper.GetSituation(type);

                switch (situcation)
                {
                    case Situation.Serializable:
                        {
                            code += Environment.NewLine +
                                    string.Format(" this.{0}=info.{1}(\"{0}\");", item.Name,
                                                  SituationHelper.GetMethodName(type));
                            break;
                        }
                    case Situation.Array:
                    case Situation.SerializableArray:
                        {
                            code += Environment.NewLine +
                                    string.Format(" this.{0}=({1})info.GetValue(\"{0}\",typeof({1}));", item.Name,
                                                  SituationHelper.GetMethodName(type, false));
                            break;
                        }
                    case Situation.UnSerializable:
                        {
                            code += Environment.NewLine +
                                    string.Format(" this.{0}=({1})info.GetValue(\"{0}\",typeof({1}));", item.Name,
                                                  SituationHelper.GetMethodName(type, false));
                            break;
                        }
                    case Situation.SerializableNullableT:
                        {
                            code += Environment.NewLine +
                                    string.Format(
                                        " if (info.GetValue(\"{0}\",typeof(object))!=null) this.{0}=info.{1}(\"{0}\");",
                                        item.Name, SituationHelper.GetMethodName(type.GetGenericArguments()[0]));
                            break;
                        }
                    case Situation.IEnumableT:
                    case Situation.SerializableIEnumableT:
                        {
                            string enumableType = type.GetGenericTypeDefinition().Name.Substring(0,
                                                                                                 type.
                                                                                                     GetGenericTypeDefinition
                                                                                                     ().Name.Length - 2);

                            code += Environment.NewLine +
                                    string.Format(" this.{0}=({1}<{2}>)info.GetValue(\"{0}\",typeof({1}<{2}>));",
                                                  item.Name, enumableType,
                                                  SituationHelper.GetInternalTypeFormArray(type)[0].Name);
                            break;
                        }

                    case Situation.Dictionary:
                    case Situation.SerializableDirtionary:
                        {
                            string enumableType = type.GetGenericTypeDefinition().Name.Substring(0,
                                                                                                 type.
                                                                                                     GetGenericTypeDefinition
                                                                                                     ().Name.Length - 2);

                            code += Environment.NewLine +
                                    string.Format(" this.{0}=({1}<{2}>)info.GetValue(\"{0}\",typeof({1}<{2},{3}>));",
                                                  item.Name, enumableType,
                                                  SituationHelper.GetInternalTypeFormArray(type)[0].Name,
                                                  SituationHelper.GetInternalTypeFormArray(type)[1].Name);
                            break;
                        }
                }
            }
            constructor.Statements.Add(new CodeSnippetStatement(code));
            newEntity.Members.Add(constructor);

            // EntityTypes.Add(parameterType);
            // Namespaces.Add(parameterType.Namespace);
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