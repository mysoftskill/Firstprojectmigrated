namespace Microsoft.PrivacyServices.AnaheimId
{
    /// <summary>
    /// Azure Public Regions, currently using in Compliance Services.
    /// To get a full list and corresponding name run this cmd: az account list-locations -o table.
    /// </summary>
    public enum AzureRegion
    {
        /// <summary>
        /// (US) East US.
        /// </summary>
        EastUS,

        /// <summary>
        /// (US) East US 2.
        /// </summary>
        EastUS2,

        /// <summary>
        /// (US) South Central US.
        /// </summary>
        SouthCentralUS,

        /// <summary>
        /// (US) West US 2.
        /// </summary>
        WestUS2,

        /// <summary>
        /// (US) West US.
        /// </summary>
        WestUS,

        /// <summary>
        /// (US) West Central US.
        /// </summary>
        WestCentralUS,

        /// <summary>
        /// Local ONEBOX, not Azure
        /// </summary>
        Local,
    }
}
