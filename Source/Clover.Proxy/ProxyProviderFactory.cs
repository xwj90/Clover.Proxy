namespace Clover.Proxy
{
    public class ProxyProviderFactory
    {
        internal static ProxyProviderBase CreateProvider (ProxyConfiguration proxyConfiguration)
        {
            switch (proxyConfiguration.ProxyType)
            {
                case ProxyType.Remote:
                    {
                        return new RemoteDomainProxyProvider(proxyConfiguration);
                    }
                default:
                    {
                        return new DefaultProxyProvider(proxyConfiguration);
                    }
            }
        }
    }
}