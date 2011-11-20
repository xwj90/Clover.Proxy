using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clover.Proxy
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class IgnoreProxyAttribute : Attribute
    {
    }
}
