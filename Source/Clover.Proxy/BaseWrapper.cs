 
using System;
using System.Reflection;
using System.CodeDom;
using System.Linq;
using System.IO;
using System.Xml;
namespace Clover.Proxy
{
    [Serializable]
    public class BaseWrapper<T> : MarshalByRefObject where T : IWrapper
    {

        private static Assembly EntityAssembly = null;
        private static Assembly LocalAssembly = null;
        private static Assembly RemoteAssembly = null;
        private static AppDomain _RemoteDomain;
        static BaseWrapper()
        {
            try
            {
                EntityAssembly = AssemblyHelper<T>.CreateEntityAssembly();
                LocalAssembly = AssemblyHelper<T>.CreateLocalAssembly(EntityAssembly);
                RemoteAssembly = AssemblyHelper<T>.CreateRemoteAssembly(EntityAssembly);
                var obj = RemoteT;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static Type CurrentType = typeof(T);

        /// <summary>
        /// user proxy model to call method 
        /// </summary>
        public static T Proxy
        {
            get
            {
                Type tempType = LocalAssembly.GetType(TypeInformation.GetLocalProxyClassFullName(CurrentType));
                return (T)Activator.CreateInstance(tempType);
            }
        }
        // private static T _remoteT;o
        /// <summary>
        /// the remote t store in other domain 
        /// </summary>
        public static T RemoteT
        {
            get
            {
                T _remoteT = default(T);

                if (_remoteT == null)
                {
                    try
                    {
                        if (_RemoteDomain == null)
                        {
                            AppDomainSetup appdomainSetup = new AppDomainSetup();
                            string filePath = Path.GetDirectoryName(AssemblyHelper<T>.DllCachePath) + @"\" + CurrentType.FullName + ".config";

                            if (File.Exists(filePath))
                            {
                                appdomainSetup.ConfigurationFile = filePath;
                            }
                            else
                            {
                                using (Stream templateStream = CurrentType.Assembly.GetManifestResourceStream(CurrentType.Namespace + ".Wrapper.Template.config"))
                                {
                                    if (templateStream != null)
                                    {
                                        using (Stream stream = CurrentType.Assembly.GetManifestResourceStream(CurrentType.Namespace + "." + CurrentType.Name + ".config"))
                                        {
                                            XmlDocument xmlDoc = new XmlDocument();
                                            xmlDoc.Load(templateStream);
                                            if (stream != null)
                                            {
                                                XmlDocument xmlDoc2 = new XmlDocument();
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
                            var evidence = AppDomain.CurrentDomain.Evidence;

                            _RemoteDomain = AppDomain.CreateDomain("Domain Application " + CurrentType.Name, evidence, appdomainSetup);
                            //_RemoteDomain.SetThreadPrincipal(ServiceContext.
                        }

                        _remoteT = (T)_RemoteDomain.CreateInstanceAndUnwrap(RemoteAssembly.FullName, TypeInformation.GetRemoteProxyClassFullName(CurrentType));
                    }
                    catch
                    {
                        if (_RemoteDomain != null)
                            AppDomain.Unload(_RemoteDomain);
                        _RemoteDomain = null;
                        _remoteT = default(T);
                        throw;
                    }

                }


                return _remoteT;

            }
        }
    }

}
