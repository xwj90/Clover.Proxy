using System;
using System.Configuration;

namespace Clover.Proxy.OldDesign
{
    internal static class Configuration
    {
        public static bool EnableRemoteDomain
        {
            get { return Convert.ToBoolean(ConfigurationManager.AppSettings["Clover.CrossDomain"]); }
        }
    }
}