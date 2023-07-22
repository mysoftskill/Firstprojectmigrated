namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;

    /// <summary>
    /// Defines basic configuration parameters used in the AId Azure Function
    /// </summary>
    public interface IAIdFunctionConfiguration : IBaseConfiguration
    {
        /// <summary>
        /// Gets Managed Identity Id for the AId Azure Function
        /// </summary>
        string AIdUamiId { get; set; }

        /// <summary>
        /// Redis Cache Endpoint
        /// </summary>
        string RedisCacheEndpoint { get; set; }

        /// <summary>
        /// Redis Cache Port
        /// </summary>
        int RedisCachePort { get; set; }

        /// <summary>
        /// Redis Cache Password Name
        /// </summary>
        string RedisCachePasswordName { get; set; }

        /// <summary>
        /// KeyVault that stores RedisPassword
        /// </summary>
        string RedisPasswordKeyVaultEndpoint { get; set;}
    }
}
