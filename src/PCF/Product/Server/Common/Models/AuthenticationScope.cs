namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Defines the different caller scopes for PCF APIs
    /// </summary>
    public enum AuthenticationScope
    {
        /// <summary>
        /// PCF Agents
        /// </summary>
        Agent,

        /// <summary>
        /// Debug Api callers
        /// </summary>
        DebugApis,

        /// <summary>
        /// PXS Service
        /// </summary>
        PxsService,

        /// <summary>
        ///  Services allowed to call GetFullCommandStatus
        /// </summary>
        GetFullCommandStatus,

        /// <summary>
        /// Services allowed to call ExportStorageGetAccounts
        /// </summary>
        ExportStorageGetAccounts,

        /// <summary>
        /// PCF Test scope
        /// </summary>
        TestHooks,
    }
}
