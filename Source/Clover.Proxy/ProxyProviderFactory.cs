namespace Clover.Proxy
{
    internal static class ProxyProviderFactory
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