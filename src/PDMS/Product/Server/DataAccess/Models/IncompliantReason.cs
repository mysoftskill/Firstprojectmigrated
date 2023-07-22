namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Enum describing the asset group incompliant reason.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IncompliantReason
    {
        /// <summary>
        /// Corresponds to the reason that no asset groups are found based on the provided asset qualifier.
        /// </summary>
        AssetGroupNotFound = 1,

        /// <summary>
        /// Corresponds to the reason that the found asset group is not linked to any delete agent.
        /// </summary>
        DeleteAgentNotFound = 2,

        /// <summary>
        /// Corresponds to the reason that the found asset group is not linked to any delete agent that's PROD enabled.
        /// </summary>
        DeleteAgentNotEnabledForProd = 3,
    }
}