using System;
using System.Collections.Concurrent;
using System.Reflection;
using Clover.Proxy;
using System.IO;
using System.Xml;
using System.Security.Policy;

namespace Clover.Proxy
{

    [Serializable]
    public class RemoteRunner<T> : MarshalByRefObject //where T : IWrapper
    {
        private static Assembly EntityAssembly;
        private static Assembly RemoteAssembly;
        private static Lazy<AppDomain> domainInitializer;

        public static AppDomain Domain
        {
            get { return domainInitializer.Value; }
        }

        static RemoteRunner()
        {
            Init();
        }

        public static T RemoteT
        {
            get
            {
                T _remoteT = default(T);
                try
                {
                    _remoteT = (T)Domain.CreateInstanceAndUnwrap(RemoteAssembly.FullName, TypeInformation.GetRemoteProxyClassFullName(typeof(T)));
                }
                catch
                {
                    if (Domain != null)
                        AppDomain.Unload(Domain);
                    InitDomain();
                    throw;
                }


                return _remoteT;
            }
        }

        private static void Init()
        {
            try
            {
                var config = new ProxyConfiguration();
                domainInitializer = new Lazy<AppDomain>(InitDomain);
                string path = config.DllCachedPath + typeof(T).FullName + ".Entity.dll";
                if (File.Exists(path))
                    EntityAssembly = Assembly.LoadFile(path);
                RemoteAssembly = AssemblyGenerator.CreateRemoteAssembly(typeof(T), config, EntityAssembly);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static AppDomain InitDomain()
        {
            Type type = typeof(T);
            var appdomainSetup = new AppDomainSetup();
            string filePath = Path.GetDirectoryName(new ProxyConfiguration().DllCachedPath) + @"\" + type.FullName + ".config";

            if (File.Exists(filePath))
            {
                appdomainSetup.ConfigurationFile = filePath;
            }
            else
            {
                using (Stream templateStream = type.Assembly.GetManifestResourceStream(type.Namespace + ".Wrapper.Template.config"))
                {
                    if (templateStream != null)
                    {
                        using (Stream stream = type.Assembly.GetManifestResourceStream(type.Namespace + "." + type.Name + ".config"))
                        {
                            var sourceDocument = new XmlDocument();
                            sourceDocument.Load(templateStream);
                            if (stream != null)
                            {
                                var targetDocument = new XmlDocument();
                                targetDocument.Load(stream);
                                ConfigurationFileHelper.Merge(ref sourceDocument, ref targetDocument);
                            }

                            File.WriteAllText(filePath, sourceDocument.OuterXml);
                            appdomainSetup.ConfigurationFile = filePath;
                        }
                    }
                }
            }

            appdomainSetup.ApplicationBase = new ProxyConfiguration().DllCachedPath;
            Evidence evidence = AppDomain.CurrentDomain.Evidence;

            AppDomain domain = AppDomain.CreateDomain("Domain Application " + type.Name, evidence, appdomainSetup);
            return domain;
        }
    }
}