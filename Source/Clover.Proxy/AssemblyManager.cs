using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Clover.Proxy.OldDesign;
using Microsoft.CSharp;
using System.Linq;
namespace Clover.Proxy
{
    internal class AssemblyManager 
    {
     

    }


    public enum AssemblyGenerationType
    {
        Default=0,
        LocalProxy=1,
        Entity=2,
        RemoteProxy=3,
    }
}