namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a request to link a specific delete agent to a collection of asset groups for certain capabilities.
    /// </summary>
    public class SharingRequest : Entity
    {
        /// <summary>
        /// The id of the owner pulled from the requesting the asset groups.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The delete agent id that should be linked to the asset groups.
        /// </summary>
        [JsonProperty(PropertyName = "deleteAgentId")]
        public string DeleteAgentId { get; set; }

        /// <summary>
        /// The name of the owner for the asset groups. All asset groups must share the same owner within a single request.
        /// </summary>
        [JsonProperty(PropertyName = "ownerName")]
        public string OwnerName { get; set; }

        /// <summary>
        /// The set of links that are being requested.
        /// </summary>
        [JsonProperty(PropertyName = "relationships")]
        public IEnumerable<SharingRelationship> Relationships { get; set; }
    }
}