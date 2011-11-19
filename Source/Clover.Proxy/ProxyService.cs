using System;
using System.Collections.Concurrent;

namespace Clover.Proxy
{
    public class ProxyService
    {
        private readonly ConcurrentDictionary<Type, IProxyProvider> TypeConfigurations =
            new ConcurrentDictionary<Type, IProxyProvider>();

        public event Action<object[]> BeforeCall;
        public event Action AfterCall;

        public T Create<T>()
        {
            Type t = typeof (T);

            IProxyProvider provider = TypeConfigurations.GetOrAdd(t,
                                                                  ProxyProviderFactory.CreateProvider(
                                                                      ProxyConfiguration.CreateByType(t)));
            provider.BeforeCall += provider_BeforeCall;

            return provider.CreateInstance<T>(); //we should return proxy class
        }

        private void provider_BeforeCall(object[] obj)
        {
            BeforeCall(obj);
        }

        //public static T Create<T>()
        //{
        //    Type t = typeof(T);

        //    var provider = TypeConfigurations.GetOrAdd(t, ProxyProviderFactory.CreateProvider(ProxyConfiguration.CreateByType(t)));

        //    return provider.CreateInstance<T>();//we should return proxy class
        //}
    }
}