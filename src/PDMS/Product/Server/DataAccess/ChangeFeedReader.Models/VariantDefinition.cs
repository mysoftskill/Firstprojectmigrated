namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Newtonsoft.Json;

    /// <summary>
    /// The definition for each NGP variant that needs to be approved by CELA or authorized persons.
    /// </summary>
    public class VariantDefinition
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
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name. This is a human readable value for display purposes.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description. This value is an optional value. Use this to provide any additional information that can help explain the entity.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// The variant definition data types.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypes")]
        public IEnumerable<string> DataTypes { get; set; }

        /// <summary>
        /// The variant definition capabilities.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }

        /// <summary>
        /// The variant definition subject types.
        /// </summary>
        [JsonProperty(PropertyName = "subjectTypes")]
        public IEnumerable<string> SubjectTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is deleted.
        /// </summary>
        [JsonProperty(PropertyName = "isDeleted")]
        public bool IsDeleted { get; set; }

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

        /// <summary>
        /// Gets or sets a value that provides sequencing information.
        /// </summary>
        [JsonProperty(PropertyName = "_lsn")]
        public long LogicalSequenceNumber { get; set; }
    }
}