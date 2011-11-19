using System;
using System.Collections.Concurrent;

namespace Clover.Proxy
{
    public class ProxyService
    {
        private readonly ConcurrentDictionary<Type, ProxyProviderBase> TypeConfigurations =
            new ConcurrentDictionary<Type, ProxyProviderBase>();

        public Action<object[]> BeforeCall;
        public Action AfterCall;

        public T Create<T>()
        {
            Type t = typeof(T);

            ProxyProviderBase provider = TypeConfigurations.GetOrAdd(t,
                                                                  ProxyProviderFactory.CreateProvider(
                                                                      ProxyConfiguration.CreateByType(t)));
            provider.BeforeCall = BeforeCall;
            provider.AfterCall = AfterCall;

            return provider.CreateInstance<T>();
        }


    }
}