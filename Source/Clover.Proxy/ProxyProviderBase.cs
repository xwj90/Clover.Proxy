using System;

namespace Clover.Proxy
{
    public class ProxyProviderBase
    {
        protected ProxyConfiguration config;

        public ProxyProviderBase(ProxyConfiguration config)
        {
            this.config = config;
            //todo:ProxyConfiguration修改
            this.BeforeCall = config.BeforeCall;
            this.AfterCall = config.AfterCall;
        }
        public ProxyProviderBase() { }
        #region IProxyProvider Members

        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }

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
        #endregion
    }
}