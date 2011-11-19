namespace Clover.Proxy
{
    public class ProxyProviderFactory
    {
        internal static IProxyProvider CreateProvider(ProxyConfiguration proxyConfiguration)
        {
            return new DefaultProxyProvider(proxyConfiguration);
        }
    }
}