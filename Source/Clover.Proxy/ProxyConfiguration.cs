//////////////////
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Clover.Proxy
{
    public class ProxyConfiguration
    {
        public static ProxyConfiguration CreateByType(Type t)
        {
            return new ProxyConfiguration();
        }


        public string DllCachedPath { get; set; }
        public List<string> Namespaces { get; set; }
        public Assembly InterfaceAssembly { get; set; }

        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

        public bool EnableCrossDomain { get; set; }
        public bool EnableDebug { get; set; }
    }
}