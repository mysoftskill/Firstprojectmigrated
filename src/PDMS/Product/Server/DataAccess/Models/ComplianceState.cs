namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Determines the compliance state of an asset group with a specific asset qualifier.
    /// </summary>
    public class ComplianceState
    {
        /// <summary>
        /// Identifies whether or not this asset group is NGP compliant.
        /// </summary>
        [JsonProperty(PropertyName = "isCompliant")]
        public bool IsCompliant { get; set; }

        /// <summary>
        /// Provides a reason code for why the asset group is not compliant. The value is null if the asset group is compliant.
        /// </summary>
        [JsonProperty(PropertyName = "incompliantReason")]
        public IncompliantReason? IncompliantReason { get; set; }
    }
}