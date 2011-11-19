using System;

namespace Clover.Proxy
{
    public class ProxyConfiguration
    {
        public static ProxyConfiguration CreateByType(Type t)
        {
            return new ProxyConfiguration();
        }
    }
}