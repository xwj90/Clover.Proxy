using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Clover.Proxy
{
    [Serializable]
    public sealed class ProxyException : Exception
    {
        public ProxyException()
            : base()
        {
        }
        public ProxyException(string message)
            : base(message)
        {
        }
        public ProxyException(string message, Exception ex)
            : base(message, ex)
        {
        }
        
    }
}
