using System;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using System.Xml;

namespace Clover.Proxy.OldDesign
{
    [Serializable]
    public class BaseWrapper<T> : MarshalByRefObject //where T : IWrapper
    {
        private static Assembly EntityAssembly;
        private static Assembly LocalAssembly;
        private static Assembly RemoteAssembly;
        private static Lazy<AppDomain> _Domain;
        private static Type CurrentType = typeof (T);

        static BaseWrapper()
        {
            Init();
        }

        public static AppDomain Domain
        {
            get { return _Domain.Value; }
        }


        public static T Proxy
        {
            get { return Create(); }
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
                        string name = Domain.FriendlyName;
                        _remoteT =
                            (T)
                            Domain.CreateInstanceAndUnwrap(RemoteAssembly.FullName,
                                                           TypeInformation.GetRemoteProxyClassFullName(CurrentType));
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
                EntityAssembly = AssemblyHelper<T>.CreateEntityAssembly();
                LocalAssembly = AssemblyHelper<T>.CreateLocalAssembly(EntityAssembly);
                RemoteAssembly = AssemblyHelper<T>.CreateRemoteAssembly(EntityAssembly);
                InitDomain();
                T obj = RemoteT;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void InitDomain()
        {
            _Domain = new Lazy<AppDomain>(() =>
                                              {
                                                  if (Configuration.EnableRemoteDomain)
                                                  {
                                                      var appdomainSetup = new AppDomainSetup();
                                                      string filePath =
                                                          Path.GetDirectoryName(AssemblyHelper<T>.DllCachePath) + @"\" +
                                                          CurrentType.FullName + ".config";

                                                      if (File.Exists(filePath))
                                                      {
                                                          appdomainSetup.ConfigurationFile = filePath;
                                                      }
                                                      else
                                                      {
                                                          using (
                                                              Stream templateStream =
                                                                  CurrentType.Assembly.GetManifestResourceStream(
                                                                      CurrentType.Namespace + ".Wrapper.Template.config")
                                                              )
                                                          {
                                                              if (templateStream != null)
                                                              {
                                                                  using (
                                                                      Stream stream =
                                                                          CurrentType.Assembly.GetManifestResourceStream
                                                                              (CurrentType.Namespace + "." +
                                                                               CurrentType.Name + ".config"))
                                                                  {
                                                                      var xmlDoc = new XmlDocument();
                                                                      xmlDoc.Load(templateStream);
                                                                      if (stream != null)
                                                                      {
                                                                          var xmlDoc2 = new XmlDocument();
                                                                          xmlDoc2.Load(stream);

                                                                          ConfigurationFileHelper.Merge(ref xmlDoc,
                                                                                                        ref xmlDoc2);
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

                                                      AppDomain a =
                                                          AppDomain.CreateDomain(
                                                              "Domain Application " + CurrentType.Name, evidence,
                                                              appdomainSetup);
                                                      string name = a.FriendlyName;
                                                      return a;
                                                  }

                                                  return AppDomain.CurrentDomain;
                                              });
        }

        /// <summary>
        /// user proxy model to call method 
        /// </summary>
        public static T Create()
        {
            Type tempType = LocalAssembly.GetType(TypeInformation.GetLocalProxyClassFullName(CurrentType));
            return (T) Activator.CreateInstance(tempType);
        }

        //public static T CurrentT
        //{
        //    get
        //    {
        //        var t1 = Activator.CreateInstance<T>();
        //        var t = (T)AppDomain.CurrentDomain.CreateInstanceAndUnwrap(RemoteAssembly.FullName, TypeInformation.GetRemoteProxyClassFullName(CurrentType));
        //        return t;
        //    }
        //}
    }
}