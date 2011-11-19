namespace Clover.Proxy.OldDesign
{
    /// <summary>
    /// Provides the numeric information of a specified service exception.
    /// </summary>
    /// <remarks>
    /// The following table shows the definition of a 32-bit integral exception error code.
    /// <list type="table">
    /// <listheader>
    /// <item>
    /// 31 - 30
    /// </item>
    /// <item>
    /// 29
    /// </item>
    /// <item>
    /// 28
    /// </item>
    /// <item>
    /// 27 - 16
    /// </item>
    /// <item>
    /// 15 - 0
    /// </item>
    /// </listheader>
    /// <description>
    /// <item>
    /// The severity of the exception, in the service, it should be 2
    /// </item>
    /// <item>
    /// The organization code, in the service, should be 1 (non-Microsoft)
    /// </item>
    /// <item>
    /// Reserved, should be 0
    /// </item>
    /// <item>
    /// Facility code, in the service, should be 0xF00 to 0xf0f
    /// </item>
    /// <item>
    /// The error code, in the service, should be 0x0000 to 0xffff
    /// </item>
    /// </description>
    /// </list>
    /// </remarks>
    public enum ErrorCode
    {
        /// <summary>
        /// Specifies the default value for the error code.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Specifies the error code that is thrown when no application key is provided.
        /// </summary>
        NoApplicationKey = unchecked((int) 0xaf000001),

        /// <summary>
        /// Specifies the error code that is thrown when no secret key is provided.
        /// </summary>
        NoSecretKey = unchecked((int) 0xaf000002),

        /// <summary>
        /// Specifies the error code that is thrown when an input value is in an invalid format.
        /// </summary>
        InvalidInputFormat = unchecked((int) 0xaf000003),

        /// <summary>
        /// Specifies the error code that is thrown when the <c>WebOperationContext</c> is null.
        /// </summary>
        NoWebOperationContext = unchecked((int) 0xaf000004),

        /// <summary>
        /// Specifies the error code that is thrown when implementation of service provider is not found.
        /// </summary>
        NoServiceProviderAssembly = unchecked((int) 0xaf000005),

        /// <summary>
        /// Specifies the error code that is thrown when the secret token associated with does not match the application ID.
        /// </summary>
        InvalidSecretToken = unchecked((int) 0xaf000006),

        /// <summary>
        /// Specifies the error code that is thrown when sevice specified under maintenance in application configuration file.
        /// </summary>
        ServiceIsUnderMaintenance = unchecked((int) 0xaf000007),

        /// <summary>
        /// Specifies the error code that is thrown when service maintenance status is error.
        /// Please check Clover.EnableMaintenance settings in application configuration file
        /// </summary>
        ServiceMaintenanceStatusConfigureError = unchecked((int) 0xaf000008),


        /// <summary>
        /// All services definition only allow inherit signle interface, for example:  GeneralService implement IGeneralService
        /// </summary>
        ServiceDefinitionOnlyAllowImplementSingleInterface = unchecked((int) 0xaf000009),


        /// <summary>
        /// Access token is invalid
        /// </summary>
        InvalidAccessToken = unchecked((int) 0xaf00000a),

        /// <summary>
        /// Access token is empty
        /// </summary>
        NoAccessToken = unchecked((int) 0xaf00000b),


        /// <summary>
        /// Indicate the url is wrong , please check with document. 
        /// </summary>
        InvalidOperationOrService = unchecked((int) 0xaf00000c),


        /// <summary>
        /// Specifies the error code that is thrown when an error occurs during updating the entity to the database.
        /// </summary>
        CannotUpdateEntity = unchecked((int) 0xaf010001),

        /// <summary>
        /// Specifies the error code that is thrown when an error occurs during validating the entity.
        /// </summary>
        InvalidEntity = unchecked((int) 0xaf010002),

        /// <summary>
        /// Specifies the error code that is thrown when a specified entity not found.
        /// </summary>
        EntityNotFound = unchecked((int) 0xaf010003),

        /// <summary>
        /// 
        /// </summary>
        InternalCompontentException = unchecked((int) 0xaf010004),

        //   0xAF021000 – 0xAF021FFF

        /// <summary>
        /// 
        /// </summary>
        GetAnnouncements_InvalidDateRange = unchecked((int) 0xAF021001),


        //   0xAF031000 - 0xAF031FFF
        /// <summary>
        /// 
        /// </summary>
        UserRegistration_GeneralError = unchecked((int) 0xAF031001),

        /// <summary>
        /// 
        /// </summary>
        UserRegistration_UserCodeExist = unchecked((int) 0xAF031002),

        /// <summary>
        /// 
        /// </summary>
        UserRegistration_InvalidParameters = unchecked((int) 0xAF031003),

        /// <summary>
        /// 
        /// </summary>
        UserRegistration_EmailExist = unchecked((int) 0xAF031004),

        /// <summary>
        /// 
        /// </summary>
        UserRegistration_PasswordPolicy = unchecked((int) 0xAF031005),

        //   0xAF032000 – 0xAF032FFF
        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_ChangePassword = unchecked((int) 0xAF032001),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_WrongUserCodeOrPasssword = unchecked((int) 0xAF032002),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_LockedUser = unchecked((int) 0xAF032003),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideUserCode = unchecked((int) 0xAF032004),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideBUStatus = unchecked((int) 0xAF032005),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideBUActive = unchecked((int) 0xAF032006),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideUser = unchecked((int) 0xAF032007),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideUplineHierarchy = unchecked((int) 0xAF032008),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideProductAccountSetting = unchecked((int) 0xAF032009),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideProductAccountList = unchecked((int) 0xAF032010),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_ForceChangePassword = unchecked((int) 0xAF032011),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_InvalideBUSportsbookSetting = unchecked((int) 0xAF032012),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_DeniedUser = unchecked((int) 0xAF032013),

        /// <summary>
        /// 
        /// </summary>
        UserAuthentication_Other = unchecked((int) 0xAF032014),


        /// <summary>
        /// 
        /// </summary>
        GetAccountBalances_Common = unchecked((int) 0xAF052000),

        /// <summary>
        /// 
        /// </summary>
        GetAccountBalances_OnlySupport_SportBook_WalletType = unchecked((int) 0xAF052001),

        /// <summary>
        /// 
        /// </summary>
        BetStatusCheck_ParameterIsNull = unchecked((int) 0xAF042001),

        ///// <summary>
        ///// 
        ///// </summary>
        //BetStatusCheck_TransactionIdIsNotValid = unchecked((int)0xAF042002),

        /// <summary>
        /// 
        /// </summary>
        PCH_Cancel_Transaction_failed = unchecked((int) 0xAF052002),
    }
}