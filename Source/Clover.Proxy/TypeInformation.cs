using System;
using System.Reflection;

namespace Clover.Proxy
{
    static class TypeInformation
    {
        public static string GetEntityProxyClassName(MemberInfo memberInfo)
        {
            return "Serializable_" + memberInfo.Name;
        }

        public static string GetLocalProxyClassName(MemberInfo memberInfo)
        {
            return "Internal_Local_" + memberInfo.Name;
        }

        public static string GetRemoteProxyClassName(MemberInfo memberInfo)
        {
            return "Internal_Remote_" + memberInfo.Name;
        }

        public static string GetEntityNamespace(Type type)
        {
            return string.IsNullOrEmpty(type.Namespace) ? "" : type.Namespace + ".Entity";
        }

        public static string GetLocalNamespace(Type type)
        {
            return string.IsNullOrEmpty(type.Namespace) ? "" : type.Namespace + ".Local";
        }

        public static string GetRemoteNamespace(Type type)
        {
            return string.IsNullOrEmpty(type.Namespace) ? "" : type.Namespace + ".Remote";
        }

        public static string GetLocalProxyClassFullName(Type type)
        {
            return GetLocalNamespace(type) + "." + GetLocalProxyClassName(type);
        }

        public static string GetRemoteProxyClassFullName(Type type)
        {
            return GetRemoteNamespace(type) + "." + GetRemoteProxyClassName(type);
        }
    }
}