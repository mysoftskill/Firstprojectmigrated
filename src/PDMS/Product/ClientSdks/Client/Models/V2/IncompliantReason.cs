[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing the asset group incompliant reason.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<IncompliantReason>))]
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