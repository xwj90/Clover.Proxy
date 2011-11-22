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
using Clover.Proxy;
using Microsoft.CSharp;

namespace Clover.Proxy
{
    internal class DefaultProxyProvider : ProxyProviderBase
    {
        private static ConcurrentDictionary<Type, Assembly> assemblies = new ConcurrentDictionary<Type, Assembly>();

        public DefaultProxyProvider(ProxyConfiguration config)
            : base(config)
        {
        }

        public override T CreateInstance<T>()
        {
            if (ProxyConfig.DisableAutoProxy)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            var type = typeof(T);
            var assembly = assemblies.GetOrAdd(type, (t) =>
            {
                return AssemblyGenerator.CreateLocalClassAssembly(type, proxyConfig);
            });
            Type proxyType = assembly.GetType(TypeInformation.GetLocalProxyClassFullName(type));
            return (T)Activator.CreateInstance(proxyType, new Object[] { this });
        }
    }
}