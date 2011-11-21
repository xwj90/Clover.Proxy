using System;
using System.Collections.Concurrent;
using System.Reflection;
using Clover.Proxy.OldDesign;
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
        private static Lazy<AppDomain> _Domain = new Lazy<AppDomain>(InitDomain);

        public static AppDomain Domain
        {
            get { return _Domain.Value; }
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

                if (_remoteT == null)
                {
                    try
                    {
                        string name = Domain.FriendlyName;
                        _remoteT = (T)Domain.CreateInstanceAndUnwrap(RemoteAssembly.FullName, TypeInformation.GetRemoteProxyClassFullName(typeof(T)));
                    }
                    catch
                    {
                        if (Domain != null)
                            AppDomain.Unload(Domain);
                        InitDomain();
                        _remoteT = default(T);
                        throw;
                    }
                }

                return _remoteT;
            }
        }

        private static void Init()
        {
            try
            {
                var config = new ProxyConfiguration();
                EntityAssembly = Assembly.LoadFile(config.DllCachedPath + typeof(T).FullName + ".Entity.dll"); //AssemblyGenerator.CreateEntityAssembly(typeof(T), new ProxyConfiguration());
                //LocalAssembly = AssemblyHelper<T>.CreateLocalAssembly(EntityAssembly);
                RemoteAssembly = AssemblyGenerator.CreateRemoteAssembly(typeof(T), config, EntityAssembly);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static AppDomain InitDomain()
        {
            Type CurrentType = typeof(T);
            var appdomainSetup = new AppDomainSetup();
            string filePath = Path.GetDirectoryName(AssemblyHelper<T>.DllCachePath) + @"\" + CurrentType.FullName + ".config";

            if (File.Exists(filePath))
            {
                appdomainSetup.ConfigurationFile = filePath;
            }
            else
            {
                using (Stream templateStream =
                        CurrentType.Assembly.GetManifestResourceStream(CurrentType.Namespace + ".Wrapper.Template.config"))
                {
                    if (templateStream != null)
                    {
                        using (Stream stream = CurrentType.Assembly.GetManifestResourceStream(CurrentType.Namespace + "." + CurrentType.Name + ".config"))
                        {
                            var xmlDoc = new XmlDocument();
                            xmlDoc.Load(templateStream);
                            if (stream != null)
                            {
                                var xmlDoc2 = new XmlDocument();
                                xmlDoc2.Load(stream);
                                ConfigurationFileHelper.Merge(ref xmlDoc, ref xmlDoc2);
                            }

                            File.WriteAllText(filePath, xmlDoc.OuterXml);
                            appdomainSetup.ConfigurationFile = filePath;
                        }
                    }
                    else
                    {
                        // appdomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                    }
                }
            }
            //Mem.ServiceAccount}
            appdomainSetup.ApplicationBase = AssemblyHelper<T>.DllCachePath;
            Evidence evidence = AppDomain.CurrentDomain.Evidence;

            AppDomain a = AppDomain.CreateDomain("Domain Application " + CurrentType.Name, evidence, appdomainSetup);
            string name = a.FriendlyName;
            return a;


        }


    }
}