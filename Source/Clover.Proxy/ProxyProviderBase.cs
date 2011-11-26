using System;

namespace Clover.Proxy
{
    public abstract class ProxyProviderBase
    {
        protected ProxyConfiguration ProxyConfig { get; set; }
        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

        public ProxyProviderBase() : this(null) { }

        public ProxyProviderBase(ProxyConfiguration config)
        {
            this.ProxyConfig = config;
            if (config != null)
            {
                this.BeforeCall = config.BeforeCall;
                this.AfterCall = config.AfterCall;
            }
        }

        public abstract T CreateInstance<T>();

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