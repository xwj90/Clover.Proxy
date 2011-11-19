using System;

namespace Clover.Proxy
{
    public interface IProxyProvider
    {
        Action<object[]> BeforeCall { get; set; }
        Action AfterCall { get; set; }
        T CreateInstance<T>();
    }
}