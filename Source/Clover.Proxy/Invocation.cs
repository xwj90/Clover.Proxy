using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Clover.Proxy
{
    public class Invocation
    {
        private object[] arguments;
        private MethodInfo proxiedMethod;
        private object proxyObject;
        public Invocation(object[] arguments, MethodInfo proxiedMethod, object proxyObject)
        {
            this.arguments = arguments;
            this.proxiedMethod = proxiedMethod;
            this.proxyObject = proxyObject;
        }
        public object[] Arguments { get { return arguments; } }
        public MethodInfo ProxiedMethod { get { return proxiedMethod; } }
        public object ProxyObject { get { return proxyObject; } }
        public object ReturnValue { get; set; }
    }
}
