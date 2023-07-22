[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;

    /// <summary>
    /// The asset group registration status.
    /// </summary>
    public class AssetGroupRegistrationStatus
    {
        /// <summary>
        /// The id of the asset group.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The owner id of the asset group.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// An overall summary of whether or not the asset group registration is complete.
        /// </summary>
        [JsonProperty(PropertyName = "isComplete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// The qualifier for the asset group.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        [JsonConverter(typeof(AssetQualifierConverter))]
        public AssetQualifier Qualifier { get; set; }

        /// <summary>
        /// The set of asset registration statuses for all assets linked to this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "assets")]
        public IEnumerable<AssetRegistrationStatus> Assets { get; set; }

        /// <summary>
        /// An overall summary of whether or not all asset registrations are correct.
        /// </summary>
        [JsonProperty(PropertyName = "assetsStatus")]
        public RegistrationState AssetsStatus { get; set; }
    }
}