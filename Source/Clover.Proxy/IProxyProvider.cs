using System;

namespace Clover.Proxy
{
    public interface IProxyProvider
    {
        event Action<object[]> BeforeCall;
        event Action AfterCall;
        T CreateInstance<T>();
    }
}