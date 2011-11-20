using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using System.Configuration;
namespace Clover.Proxy
{
    public class ProxyConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public bool DisableAutoProxy { get; set; }
        public bool EnableDebug { get; set; }
        public bool EnableCrossDomain { get; set; }
        public string DllCachedPath { get; set; }
        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }
        public ProxyType ProxyType { get; set; }
        public Dictionary<string, bool> MemberAutoProxyStatus = new Dictionary<string, bool>();

        private static ConcurrentDictionary<Type, ProxyConfiguration> configurations = new ConcurrentDictionary<Type, ProxyConfiguration>();


        public ProxyConfiguration()
            : this(null)
        {

        }
        private ProxyConfiguration(ProxyAttribute attribute)
        {
            if (attribute == null)
            {
                this.DisableAutoProxy = Convert.ToBoolean(ConfigurationManager.AppSettings["Clover.Proxy.DisableAutoProxy"]);
                this.EnableDebug = Convert.ToBoolean(ConfigurationManager.AppSettings["Clover.Proxy.EnableDebug"]);
                this.DllCachedPath = Convert.ToString(ConfigurationManager.AppSettings["Clover.Proxy.DllCachedPath"]);
                this.ProxyType = (ProxyType)Enum.Parse(typeof(ProxyType), Convert.ToString(ConfigurationManager.AppSettings["Clover.Proxy.ProxyType"]));
            }
            else
            {
                this.DisableAutoProxy = attribute.DisableAutoProxy;
                this.EnableDebug = attribute.EnableDebug;
                this.DllCachedPath = attribute.DllCachedPath;
                this.BeforeCall = attribute.BeforeCall;
                this.AfterCall = attribute.AfterCall;
                this.ProxyType = attribute.ProxyType;
            }
            if (string.IsNullOrWhiteSpace((this.DllCachedPath)))
            {
                this.DllCachedPath = AppDomain.CurrentDomain.BaseDirectory;
            }
        }
        public static ProxyConfiguration Create(Type type)
        {
            return configurations.GetOrAdd(type, (t) =>
            {
                object[] args = t.GetCustomAttributes(typeof(ProxyAttribute), true);
                ProxyConfiguration config = null;
                if (args.Length > 0)
                {
                    config = new ProxyConfiguration(args[0] as ProxyAttribute);
                }
                else
                {
                    config = new ProxyConfiguration();
                }

                foreach (var item in type.GetMembers())
                {
                    object[] memberStatus = item.GetCustomAttributes(typeof(ProxyAttribute), true);
                    if (memberStatus.Length > 0)
                    {
                        config.MemberAutoProxyStatus[item.Name] = (memberStatus[0] as ProxyAttribute).DisableAutoProxy;
                    }
                }

                if (string.IsNullOrWhiteSpace((config.DllCachedPath)))
                {
                    config.DllCachedPath = AppDomain.CurrentDomain.BaseDirectory;
                }
                return config;
            });
        }
    }

    public class ProxyAttribute : Attribute
    {
        public bool DisableAutoProxy { get; set; }
        public bool EnableDebug { get; set; }
        public string DllCachedPath { get; set; }
        public Action<Invocation> BeforeCall { get; set; }
        public Action<Invocation> AfterCall { get; set; }
        public ProxyType ProxyType { get; set; }

        public ProxyAttribute()
        {
            this.EnableDebug = true;
            this.DllCachedPath = null;
            this.BeforeCall = null;
            this.AfterCall = null;
            this.DisableAutoProxy = false;
            this.ProxyType = Proxy.ProxyType.Default;
        }
    }

    public enum ProxyType
    {
        Default = 0,
        Local = 0,
        Remote = 1,
    }
}