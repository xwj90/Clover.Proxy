using System;
using System.Collections.Concurrent;

namespace Clover.Proxy
{
    public class ProxyService
    {
        private readonly ConcurrentDictionary<Type, ProxyProviderBase> TypeConfigurations =
            new ConcurrentDictionary<Type, ProxyProviderBase>();

        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

        public T Create<T>()
        {
            Type t = typeof(T);

            ProxyProviderBase provider = TypeConfigurations.GetOrAdd(t,
                new Func<ProxyProviderBase>(() =>
                        {
                            var config = ProxyConfiguration.CreateByType(t);
                            config.BeforeCall = BeforeCall;
                            config.AfterCall = AfterCall;
                            var p = ProxyProviderFactory.CreateProvider(config);
                     
                            return p;
                        })());

            return provider.CreateInstance<T>();
        }


    }
}