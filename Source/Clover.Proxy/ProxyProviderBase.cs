using System;

namespace Clover.Proxy
{
    public class ProxyProviderBase : IProxyProvider
    {
        #region IProxyProvider Members

        public event Action<object[]> BeforeCall;
        public event Action AfterCall;

        public virtual T CreateInstance<T>()
        {
            throw new NotImplementedException("you have to override CreateInstance<T> method!");
        }

        #endregion
    }
}