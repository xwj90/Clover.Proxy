using System;

namespace Clover.Proxy
{
    public class ProxyProviderBase
    {
        protected ProxyConfiguration proxyConfig { get; set; }
        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

        public ProxyProviderBase() : this(null) { }

        public ProxyProviderBase(ProxyConfiguration config)
        {
            this.proxyConfig = config;
            if (config != null)
            {
                this.BeforeCall = config.BeforeCall;
                this.AfterCall = config.AfterCall;
            }
        }

        public virtual T CreateInstance<T>()
        {
            throw new NotImplementedException("you have to override CreateInstance<T> method!");
        }

        public void ExecuteBeforeCall(Invocation invocation)
        {
            if (BeforeCall != null)
            {
                BeforeCall(invocation);
            }
        }

        public void ExecuteAfterCall(Invocation invocation)
        {
            if (AfterCall != null)
            {
                AfterCall(invocation);
            }
        }
    }
}