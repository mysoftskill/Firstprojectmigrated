[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// The definition for each NGP variant that needs to be approved by CELA or authorized persons.
    /// </summary>
    public class VariantDefinition : NamedEntity
    {
        /// <summary>
        /// The variant EGRC Id.
        /// </summary>
        [JsonProperty(PropertyName = "egrcId")]
        public string EgrcId { get; set; }

        /// <summary>
        /// The variant EGRC Name.
        /// </summary>
        [JsonProperty(PropertyName = "egrcName")]
        public string EgrcName { get; set; }

        /// <summary>
        /// The variant definition data types.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypes")]
        public IEnumerable<DataTypeId> DataTypes { get; set; }

        /// <summary>
        /// The variant definition capabilities.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<CapabilityId> Capabilities { get; set; }

        /// <summary>
        /// The variant definition subject types.
        /// </summary>
        [JsonProperty(PropertyName = "subjectTypes")]
        public IEnumerable<SubjectTypeId> SubjectTypes { get; set; }

        /// <summary>
        /// The variant definition approver.
        /// </summary>
        [JsonProperty(PropertyName = "approver")]
        public string Approver { get; set; }

        /// <summary>
        /// The id of the associated data owner.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The associated data owner. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public DataOwner Owner { get; set; }

        /// <summary>
        /// The state of the variant definiton: Active or Closed.
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public VariantDefinitionState State { get; set; }

        /// <summary>
        /// Reason for Closure: only relevant when State == Closed: Intentional or Expired.
        /// </summary>
        [JsonProperty(PropertyName = "reason")]
        public VariantDefinitionReason Reason { get; set; }
    }
}