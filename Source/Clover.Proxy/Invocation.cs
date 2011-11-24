using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace Clover.Proxy
{
    public sealed class Invocation
    {
        public object[] Arguments { get; private set; }
        public MethodInfo Method { get; private set; }
        public object ProxyObject { get; private set; }
        public object ReturnValue { get; set; }

        public Invocation(object[] arguments, MethodInfo method, object value)
        {
            this.Arguments = arguments;
            this.Method = method;
            this.ProxyObject = value;
        }

        public override string ToString()
        {
            return string.Format("Arguments:{0},Method:{1},ReturnValue:{2}", string.Join(",", Arguments), Method.Name, ReturnValue);
        }
    }
}
