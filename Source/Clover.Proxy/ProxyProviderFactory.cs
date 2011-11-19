namespace Clover.Proxy
{
    public class ProxyProviderFactory
    {
        internal static ProxyProviderBase CreateProvider(ProxyConfiguration proxyConfiguration)
        {
            return new DefaultProxyProvider(proxyConfiguration);
        }
    }
}