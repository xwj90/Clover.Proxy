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
    [ComVisible(true)]
    public sealed class AssemblyProxyAttribute : Attribute
    {
        public AssemblyProxyAttribute(string proxy)
        {
            Proxy = proxy;
        }
        public string Proxy { get; set; }
    }

    public class ProxyConfigSection : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (XmlNode xnl in section.ChildNodes)
            {
                if (XmlNodeType.Element == xnl.NodeType)
                {
                    dic.Add(xnl.Attributes["key"].Value, xnl.Attributes["value"].Value);
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
                DisableAutoProxy = Convert.ToBoolean(dic["DisableAutoProxy"]);
                EnableDebug = Convert.ToBoolean(dic["EnableDebug"]);
                EnableCrossDomain = Convert.ToBoolean(dic["EnableCrossDomain"]);
                DllCachedPath = dic["DllCachedPath"];
                EnumPType = (ProxyType)Enum.Parse(typeof(ProxyType), dic["ProxyType"], true);
                IsConfiged = true;
            }
        }

    }
}
