using System;

namespace Clover.Proxy
{
    public class ProxyProviderBase
    {
        private ProxyConfiguration config;

        public ProxyProviderBase(ProxyConfiguration config)
        {
            this.config = config;
            this.BeforeCall = config.BeforeCall;
            this.AfterCall = config.AfterCall;
        }
        #region IProxyProvider Members

        public Action<object[]> BeforeCall { get; set; }
        public Action AfterCall { get; set; }

        public virtual T CreateInstance<T>()
        {
            throw new NotImplementedException("you have to override CreateInstance<T> method!");
        }


        public void ExecuteBeforeCall(object[] objs)
        {
            if (BeforeCall != null)
            {
                BeforeCall(objs);
            }
        }

        public void ExecuteAfterCall()
        {
            if (AfterCall != null)
            {
                AfterCall();
            }
        }
        #endregion
    }
}