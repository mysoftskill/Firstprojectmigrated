namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;

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
        public string Qualifier { get; set; }

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