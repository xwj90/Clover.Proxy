 
namespace Clover.Proxy
{
    #region Using directives

    using System;
    using System.Runtime.Serialization;
  

    #endregion

    /// <summary>
    /// Encapsulates the information for an authorized user. This class cannot be inherited.
    /// </summary>
    [Serializable]
    public sealed class AuthenticatedUser : MarshalByRefObject
    {
        #region Properties

        /// <summary>
        /// Gets or sets the account ID that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="long"/> that represents the account ID.
        /// </value>
        [DataMember]
        public long AccountId { get; set; }

        /// <summary>
        /// Gets or sets the time when this instance is authenticated.
        /// </summary>
        /// <value>
        /// <see cref="DateTime"/> that represents the authenticated time.
        /// </value>
        [DataMember]
        public DateTime AuthenticatedTime { get; set; }

        /// <summary>
        /// Gets or sets the business unit code that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="string"/> that represents the business unit code.
        /// </value>
        [DataMember]
        public string BusinessUnitCode { get; set; }

        /// <summary>
        /// Gets or sets the business unit ID that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="int"/> that represents the business unit ID.
        /// </value>
        [DataMember]
        public int BusinessUnitId { get; set; }

        /// <summary>
        /// Gets or sets the culture info that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="string"/> that represents the culture info in the <![CDATA[<country2>-<language2>]]> format.
        /// </value>
        [DataMember]
        public string Culture { get; set; }

        /// <summary>
        /// The ip address.
        /// </summary>
        [DataMember]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the time when this instance is recently active.
        /// </summary>
        /// <value>
        /// <see cref="DateTime"/> that represents the last activity time.
        /// </value>
        [DataMember]
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// Gets or sets the access token that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="string"/> that represents the token.
        /// </value>
        [DataMember]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the user code that is associated with this instance.
        /// </summary>
        /// <value>
        /// <see cref="string"/> that represents the user code.
        /// </value>
        [DataMember]
        public string UserCode { get; set; }

        

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int CurrencyId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Lang Code
        /// </summary>
        [DataMember]
        public string LanguageCode { get; set; }

        /// <summary>
        /// Vendor Type
        /// </summary>
        [DataMember]
        public string VendorType { get; set; }

        /// <summary>
        /// Vendor Id
        /// </summary>
        [DataMember]
        public int VendorId { get; set; }

        /// <summary>
        /// Holds daily bet limit set by the user
        /// </summary>
        [DataMember]
        public decimal DailyBetLimit { get; set; }
        #endregion
    }
}
