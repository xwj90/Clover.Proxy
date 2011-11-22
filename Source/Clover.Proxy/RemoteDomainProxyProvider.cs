using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
namespace Clover.Proxy
{
    internal class RemoteDomainProxyProvider : ProxyProviderBase
    {
        private static ConcurrentDictionary<Type, Dictionary<AssemblyType, Assembly>> cachedAssemblies = new ConcurrentDictionary<Type, Dictionary<AssemblyType, Assembly>>();
        private ProxyConfiguration defaultConfiguration = null;

        public RemoteDomainProxyProvider(ProxyConfiguration config)
            : base(config)
        {
            defaultConfiguration = config;
        }

        public override T CreateInstance<T>()
        {
            var configuratio = ProxyConfiguration.Create(typeof(T));
            if (configuratio.DisableAutoProxy)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            Type type = typeof(T);
            var assemblies = cachedAssemblies.GetOrAdd(type, (t) => { return InitAssembly(t); });
            Type proxyType = assemblies[AssemblyType.Local].GetType(TypeInformation.GetLocalProxyClassFullName(typeof(T)));
            return (T)Activator.CreateInstance(proxyType, new Object[] { this });

        }
        private static Dictionary<AssemblyType, Assembly> InitAssembly(Type type)
        {
            var configuration = ProxyConfiguration.Create(type);
            Dictionary<AssemblyType, Assembly> dict = new Dictionary<AssemblyType, Assembly>();

            dict[AssemblyType.Entity] = AssemblyGenerator.CreateEntityAssembly(type, configuration);
            dict[AssemblyType.Local] = AssemblyGenerator.CreateLocalAssembly(type, configuration, dict[AssemblyType.Entity]);
           // dict[AssemblyType.Remote] = AssemblyGenerator.CreateRemoteAssembly(type, configuration, dict[AssemblyType.Entity]);
            return dict;
        }
       
 

        private enum AssemblyType
        {
            Default = 0,
            Local = 1,
            Entity = 2,
            Remote = 4,
        }
    }
}