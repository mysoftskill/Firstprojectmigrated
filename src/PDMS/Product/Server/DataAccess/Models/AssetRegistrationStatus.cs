namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The asset registration status.
    /// </summary>
    public class AssetRegistrationStatus
    {
        /// <summary>
        /// The id of the asset from DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// An overall summary of whether or not the asset registration is complete.
        /// </summary>
        [JsonProperty(PropertyName = "isComplete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// The qualifier for the asset.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        public string Qualifier { get; set; }

        /// <summary>
        /// Whether or not the NonPersonal tag was found.
        /// </summary>
        [JsonProperty(PropertyName = "isNonPersonal")]
        public bool IsNonPersonal { get; set; }

        /// <summary>
        /// Whether or not the LongTail or CustomNonUse tag was found.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TailOr")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NonUse")]
        [JsonProperty(PropertyName = "isLongTailOrCustomNonUse")]
        public bool IsLongTailOrCustomNonUse { get; set; }

        /// <summary>
        /// The subject type tags for the asset.
        /// </summary>
        [JsonProperty(PropertyName = "subjectTypeTags")]
        public IEnumerable<Tag> SubjectTypeTags { get; set; }

        /// <summary>
        /// The subject type tag registration status.
        /// </summary>
        [JsonProperty(PropertyName = "subjectTypeTagsStatus")]
        public RegistrationState SubjectTypeTagsStatus { get; set; }

        /// <summary>
        /// The data type tag registration status.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeTags")]
        public IEnumerable<Tag> DataTypeTags { get; set; }

        /// <summary>
        /// The data type tag registration status.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeTagsStatus")]
        public RegistrationState DataTypeTagsStatus { get; set; }
    }
}