namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the authentication mechanism used for interacting with the agent.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<AuthenticationType>))]
    public enum AuthenticationType
    {
        /// <summary>
        /// This uses MSA S2S tickets for authentication.
        /// </summary>
        MsaSiteBasedAuth = 2,

        /// <summary>
        /// This uses AAD application tickets for authentication.
        /// </summary>
        AadAppBasedAuth = 4
    }
}