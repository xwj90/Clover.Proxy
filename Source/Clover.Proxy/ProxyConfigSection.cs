using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Runtime.InteropServices;

namespace Clover.Proxy
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]  
    [ComVisible(false)]
    public sealed class AssemblyProxyAttribute : Attribute
    {
        public AssemblyProxyAttribute(string proxy)
        {
            Proxy = proxy;
        }
        public string Proxy { get; private set; }
    }

    public class ProxyConfigSection : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (section!=null)
            {
                if (section.HasChildNodes)
                {
                    foreach (XmlNode xnl in section.ChildNodes)
                    {
                        if (XmlNodeType.Element == xnl.NodeType)
                        {
                            dic.Add(xnl.Attributes["key"].Value, xnl.Attributes["value"].Value);
                        }
                    }
                }
            }
            return dic;
        }

        public static bool DisableAutoProxy { get; private set; }
        public static bool EnableDebug { get; private set; }
        public static bool EnableCrossDomain { get; private set; }
        public static string DllCachedPath { get; private set; }
        public static ProxyType EnumPType { get; private set; }
        public static bool IsConfiged { get; private set; }
        static ProxyConfigSection()
        {
            IsConfiged = false;
            if (ConfigurationManager.GetSection("Clover.Proxy") != null)
            {
                Dictionary<string, string> dic = (Dictionary<string, string>)ConfigurationManager.GetSection("Clover.Proxy");
                ProxyFormatProvider boolFormat = new ProxyFormatProvider();
                DisableAutoProxy = Convert.ToBoolean(dic["DisableAutoProxy"], boolFormat);
                EnableDebug = Convert.ToBoolean(dic["EnableDebug"], boolFormat);
                EnableCrossDomain = Convert.ToBoolean(dic["EnableCrossDomain"], boolFormat);
                DllCachedPath = dic["DllCachedPath"];
                EnumPType = (ProxyType)Enum.Parse(typeof(ProxyType), dic["ProxyType"], true);
                IsConfiged = true;
            }
        }
    }

    public class ProxyFormatProvider : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return false;
        }
    }
}
