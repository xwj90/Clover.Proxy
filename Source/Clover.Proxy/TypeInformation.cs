 
using System;
using System.Reflection;
using System.Collections.Generic;
namespace Clover.Proxy
{


    public class TypeInformation
    {
        public static string GetEntityProxyClassName(Type t)
        {
            return "Serializable_" + t.Name;
        }
        public static string GetLocalProxyClassName(Type t)
        {
            return "Internal_Local_" + t.Name;
        }

        public static string GetRemoteProxyClassName(Type t)
        {
            return "Internal_Remote_" + t.Name;
        }
        public static string GetEntityNamespace(Type t)
        {
            return string.IsNullOrEmpty(t.Namespace) ? "" : t.Namespace + ".Entity";
        }

        public static string GetLocalNamespace(Type t)
        {
            return string.IsNullOrEmpty(t.Namespace) ? "" : t.Namespace + ".Local";
        }
        public static string GetRemoteNamespace(Type t)
        {
            return string.IsNullOrEmpty(t.Namespace) ? "" : t.Namespace + ".Remote";
        }

        public static string GetLocalProxyClassFullName(Type t)
        {
            return GetLocalNamespace(t) + "." + GetLocalProxyClassName(t);
        }
        public static string GetRemoteProxyClassFullName(Type t)
        {
            return GetRemoteNamespace(t) + "." + GetRemoteProxyClassName(t);
        }

    }

}
