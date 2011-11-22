using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clover.Proxy
{
    public class ProxyException : ApplicationException
    {
        public ProxyException(string message)
            : base(message)
        {
        }
    }
}
