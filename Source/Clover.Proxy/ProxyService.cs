using System;
using System.Collections.Concurrent;

namespace Clover.Proxy
{
    public class ProxyService
    {
        private readonly ConcurrentDictionary<Type, ProxyProviderBase> TypeConfigurations =
            new ConcurrentDictionary<Type, ProxyProviderBase>();
        private readonly object createProviderLock = new object();

        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

        private ProxyProviderBase CreateProvider(Type type)
        {
            lock (createProviderLock)
            {
                ProxyProviderBase result;
                if (TypeConfigurations.TryGetValue(type, out result)) return result;
                var config = ProxyConfiguration.Create(type);
                config.BeforeCall = BeforeCall;
                config.AfterCall = AfterCall;
                result = ProxyProviderFactory.CreateProvider(config);
                TypeConfigurations[type] = result;
                return result;
            }
        }
        public T Create<T>()
        {
            Type type = typeof(T);

            ProxyProviderBase provider = TypeConfigurations.GetOrAdd(type, (t) => CreateProvider(t));

            return provider.CreateInstance<T>();
        }
    }
}