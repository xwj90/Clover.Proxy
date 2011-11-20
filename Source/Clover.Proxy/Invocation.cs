using System.Reflection;

namespace Clover.Proxy
{
    public sealed class Invocation
    {
        public object[] Arguments { get; private set; }
        public MethodInfo ProxiedMethod { get; private set; }
        public object ProxyObject { get; private set; }
        public object ReturnValue { get; set; }

        public Invocation(object[] arguments, MethodInfo proxiedMethod, object proxyObject)
        {
            this.Arguments = arguments;
            this.ProxiedMethod = proxiedMethod;
            this.ProxyObject = proxyObject;
        }
    }
}
