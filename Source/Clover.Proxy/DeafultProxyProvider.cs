using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Clover.Proxy;
using Microsoft.CSharp;

namespace Clover.Proxy
{
    internal class DefaultProxyProvider : ProxyProviderBase
    {
        private readonly string DLLCachedPath = AppDomain.CurrentDomain.BaseDirectory;
        private static ConcurrentDictionary<Type, Assembly> assemblies = new ConcurrentDictionary<Type, Assembly>();

        public DefaultProxyProvider(ProxyConfiguration config)
            : base(config)
        {
            this.DLLCachedPath = config.DllCachedPath;
        }

        public override T CreateInstance<T>()
        {
            if (ProxyConfig.DisableAutoProxy)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            var type = typeof(T);
            var assembly = assemblies.GetOrAdd(type, (t) =>
            {
                return AssemblyGenerator.CreateLocalClassAssembly(type, ProxyConfig, type.Assembly);
            });
            Type proxyType = assembly.GetType(TypeInformation.GetLocalProxyClassFullName(type));
            return (T)Activator.CreateInstance(proxyType, new Object[] { this });
        }


        /*
        private static CodeTypeReference GetSimpleType(Type t)
        {
            Situation s = SituationHelper.GetSituation(t);
            switch (s)
            {
                case Situation.Dictionary:
                case Situation.SerializableDirtionary:
                    {
                        if (t.IsGenericType)
                        {
                            return new CodeTypeReference(string.Format("{0}<global::{1},global::{2}>"
                                                                       , t.Name.Substring(0, t.Name.Length - 2)
                                                                       , t.GetGenericArguments()[0].FullName
                                                                       , t.GetGenericArguments()[1].FullName
                                                             ));
                        }
                        break;
                    }
                case Situation.IEnumableT:
                case Situation.SerializableIEnumableT:
                    {
                        if (t.IsGenericType)
                        {
                            return
                                new CodeTypeReference(string.Format("{0}<global::{1}>",
                                                                    t.Name.Substring(0, t.Name.Length - 2),
                                                                    t.GetGenericArguments()[0].FullName));
                        }
                        break;
                    }
            }


            return new CodeTypeReference(t);
        }
         * */
    }
}