 #region Using Directives

using System;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using System.Configuration;
using System.Web.Security;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;

using System.Web;
using System.Web.SessionState;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
 

#endregion
namespace Clover.Proxy
{


    /// <summary>
    /// Defines service context include user information, partner information, buCode.
    /// </summary>
    public class ServiceContext
    {
        private static string SuperAccountDomain = null;
        private static string SuperAccountName = null;
        private static string SuperAccountPassword = null;
        private static int defaultBusinessUnitId = 0;
        private static string DefaultBusinessUnitCode = null;
        private static string THREAD_DATASLOT = "AgileBetThreadData";



        static ServiceContext()
        {
            SuperAccountDomain = ConfigurationManager.AppSettings["AgileBetSdk.SuperAccountDomain"];
            SuperAccountName = ConfigurationManager.AppSettings["AgileBetSdk.SuperAccountName"];
            SuperAccountPassword = ConfigurationManager.AppSettings["AgileBetSdk.SuperAccountPassword"];
            DefaultBusinessUnitCode = ConfigurationManager.AppSettings["AgileBetSdk.DefaultBusinessUnitCode"];
            string val = ConfigurationManager.AppSettings["AgileBetSdk.DefaultBusinessUnitId"];
            int.TryParse(val, out   defaultBusinessUnitId);


        }
        public static int DefaultBusinessUnitId
        {
            get
            {
                return defaultBusinessUnitId;
            }
        }

        public static string IpAddress
        {
            get
            {
                return "127.0.0.1";
            }
        }


        public static void SetThreadForAgileBetCoreSystem()
        {
            //BaseCommon.AgileBetThreadData data = new BaseCommon.AgileBetThreadData();
            //data.BuCode = string.Empty;
            //data.BUID = DefaultBusinessUnitId;

            //LocalDataStoreSlot namedDataSlot = Thread.GetNamedDataSlot(THREAD_DATASLOT);
            //if (namedDataSlot == null)
            //{
            //    Thread.AllocateNamedDataSlot(THREAD_DATASLOT);
            //    namedDataSlot = Thread.GetNamedDataSlot(THREAD_DATASLOT);
            //}
            //Thread.SetData(namedDataSlot, data);

        }
        public static void RemoveThreadForAgileBetCoreSystem()
        {
            // if(Thread.
            // Thread.FreeNamedDataSlot(
        }


        /// <summary>
        /// indicate user who want to implement logic
        /// </summary>
        public static string UserCode
        {
            get
            {
                return User.UserCode;
            }

        }
        /// <summary>
        /// represent clent's business unit code
        /// </summary>
        public static string BusinessUnitCode
        {
            get
            {
                return User.BusinessUnitCode;
            }
        }
        /// <summary>
        /// represent clent's business unit id
        /// </summary>
        public static int BusinessUnitId
        {
            get
            {
                return User.BusinessUnitId;
            }
        }

        /// <summary>
        /// CurrentCulture for example : en-us zh-cn
        /// </summary>
        public static string Culture
        {
            get
            {
                if (User != null)
                    return User.Culture;
                return "en-sg";
            }

        }

        /// <summary>
        /// CurrentCulture for example : ENG 
        /// </summary>
        public static string InternalCultureName
        {
            get
            {
                if (User == null)
                    return "ENG";
                string val = Convert.ToString(User.Culture);
                if (val == null)
                    return "ENG";
                switch (val.ToUpper())
                {
                    case "EN-SG":
                        {
                            return "ENG";
                        }

                    default:
                        {
                            return "ENG";
                        }
                }

            }

        }

        /// <summary>
        /// current user , 
        /// </summary>
        public static AuthenticatedUser User
        {
            get
            {
                return CallContext.LogicalGetData("AuthenticatedUser") as AuthenticatedUser;
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginId"></param>
        private static void SetThreadPrincipal(string loginId)
        {
            string[] roles = Roles.GetRolesForUser(loginId);
            System.Security.Principal.IIdentity userIdentity = new System.Security.Principal.GenericIdentity(loginId);
            System.Security.Principal.IPrincipal userPrincipal = new System.Security.Principal.GenericPrincipal(userIdentity, roles);
            Thread.CurrentPrincipal = userPrincipal;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetThreadPrincipal()
        {
            string val = ConfigurationManager.AppSettings["Mem.ServiceAccount"];
            if (string.IsNullOrEmpty(val))
            {
                throw new Exception("Please Configure Mem.ServiceAccount in AppSettings Section, Application Configuration File :" + Path.GetFileName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
            }
            SetThreadPrincipal(val);

        }


        [DllImport("advapi32.dll")]
        public static extern int LogonUserA(String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        public const int LOGON32_LOGON_INTERACTIVE = 2;
        public const int LOGON32_PROVIDER_DEFAULT = 0;

        public static bool ImpersonateValidUser()
        {
            return ServiceContext.ImpersonateValidUser(SuperAccountName, SuperAccountDomain, SuperAccountPassword);
        }
        public static bool ImpersonateValidUser(String userName, String domain, String password)
        {
            ServiceContext.SetThreadPrincipal();
            WindowsImpersonationContext impersonationContext;

            WindowsIdentity tempWindowsIdentity;
            IntPtr token = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            if (RevertToSelf())
            {
                if (LogonUserA(userName, domain, password, LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT, ref token) != 0)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                        impersonationContext = tempWindowsIdentity.Impersonate();
                        if (impersonationContext != null)
                        {
                            CloseHandle(token);
                            CloseHandle(tokenDuplicate);
                            return true;
                        }
                    }
                }
            }
            if (token != IntPtr.Zero)
                CloseHandle(token);
            if (tokenDuplicate != IntPtr.Zero)
                CloseHandle(tokenDuplicate);
            return false;
        }




        private static ConcurrentDictionary<string, AuthenticatedUser> _Users = new ConcurrentDictionary<string, AuthenticatedUser>();

        public static ConcurrentDictionary<string, AuthenticatedUser> Users
        {
            get
            {
                return _Users;
            }
        }
        public static void InitTestUser()
        {
            AuthenticatedUser user = new AuthenticatedUser();
            user.UserCode = "sguoan";
            user.BusinessUnitId = 360;
            user.BusinessUnitCode = "188BET";
            user.AccountId = 123;
            user.Token = Guid.NewGuid().ToString();
            InitTestUser(user);
        }
        public static void InitTestUser(AuthenticatedUser user)
        {

            RegisterUserToContext(user, false);
        }

        public static string RegisterUserToContext(AuthenticatedUser user)
        {
            return RegisterUserToContext(user, true);
        }
        public static string RegisterUserToContext(AuthenticatedUser user, bool isNewUser)
        {
            if (user == null)
                return null;
            if (isNewUser)
            {
                string token = Guid.NewGuid().ToString();
                user.Token = token;

                user.AuthenticatedTime = DateTime.Now;
            }

            user.LastActivityTime = DateTime.Now;
            _Users[user.Token] = user;
            CallContext.LogicalSetData("AuthenticatedUser", user);
            return "token=" + user.Token + "&userid=" + user.UserCode + "&ipaddress=" + user.IpAddress;
        }

        public static void UnregisterUserFromContext()
        {
            UnregisterUserFromContext(ServiceContext.User);
        }

        public static void UnregisterUserFromContext(AuthenticatedUser user)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            UnregisterUserFromContext(user.Token);
        }

        public static void UnregisterUserFromContext(string token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            if (Users.ContainsKey(token))
            {

                AuthenticatedUser user;
                bool result = Users.TryRemove(token, out user);
                if (!result)
                {
                    throw new InvalidOperationException("unregister user from context have a exception, please try again");
                }
            }
            else
            {
                throw new InvalidOperationException(" user already unregister!");

            }
        }

        public static void FakeHttpContext()
        {
            FakeHttpContext(false);
        }
        public static void FakeHttpContext(bool includeIp)
        {
            HttpRequest request = new HttpRequest("", "http://localhost", "");
            HttpContext.Current = new HttpContext(request, new HttpResponse(new System.IO.StringWriter()));
            System.Web.SessionState.SessionStateUtility.AddHttpSessionStateToContext(
                                                                                             HttpContext.Current, new HttpSessionStateContainer
                                                                                               ("",
                                                                                                   new SessionStateItemCollection(),
                                                                                                   new HttpStaticObjectsCollection(),
                                                                                                   20000,
                                                                                                   true,
                                                                                                   HttpCookieMode.UseCookies,
                                                                                                   SessionStateMode.Off,
                                                                                                   false
                                                                                               )
                                                                                            );

            if (includeIp)
            {
                var instance = HttpContext.Current.Request.ServerVariables;
                Type type = instance.GetType();
                BindingFlags temp = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                MethodInfo addStatic = null;
                MethodInfo makeReadOnly = null;
                MethodInfo makeReadWrite = null;


                MethodInfo[] methods = type.GetMethods(temp);
                foreach (MethodInfo method in methods)
                {
                    switch (method.Name)
                    {
                        case "MakeReadWrite": makeReadWrite = method;
                            break;
                        case "MakeReadOnly": makeReadOnly = method;
                            break;
                        case "AddStatic": addStatic = method;
                            break;
                    }
                }

                makeReadWrite.Invoke(instance, null);
                List<string[]> list = new List<string[]>();
                string ip = "127.0.0.1";
                list.Add(new string[] { "HTTP_X_FORWARDED_FOR", ip });
                list.Add(new string[] { "HTTP_CLIENT_IP", ip });
                list.Add(new string[] { "HTTP_X_FORWARDED", ip });
                list.Add(new string[] { "HTTP_X_CLUSTER_CLIENT_IP", ip });
                list.Add(new string[] { "HTTP_FORWARDED_FOR", ip });
                list.Add(new string[] { "HTTP_FORWARDED", ip });
                list.Add(new string[] { "REMOTE_ADDR", ip });
                foreach (string[] values in list)
                    addStatic.Invoke(instance, values);
                makeReadOnly.Invoke(instance, null);


            }
        }



    }
}
