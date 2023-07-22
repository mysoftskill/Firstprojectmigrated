namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;

    using Microsoft.AspNet.OData.Builder;

    using Newtonsoft.Json;

    /// <summary>
    /// This is a representation of data agents that link to DataAssets in DataGrid and are used for deletion.
    /// </summary>
    public class DeleteAgent : DataAgent
    {
        /// <summary>
        /// Indicates whether or not this agent is enabled for sharing across owners.
        /// When this is enabled, the agent owner may start to receive requests
        /// for linking the agent to other owners' asset groups.
        /// </summary>
        [JsonProperty(PropertyName = "sharingEnabled")]
        public bool SharingEnabled { get; set; }

        /// <summary>
        /// Indicates whether this agent would send delete signals to a 3rd party.
        /// </summary>
        [JsonProperty(PropertyName = "isThirdPartyAgent")]
        public bool IsThirdPartyAgent { get; set; }

        /// <summary>
        /// Indicates whether or not this agent has any pending sharing requests.
        /// This is a calculated value and will be NULL unless explicitly requested
        /// in a $select statement. 
        /// </summary>
        [JsonProperty(PropertyName = "hasSharingRequests")]
        public bool? HasSharingRequests { get; set; }

        /// <summary>
        /// The associated asset groups. Must use $expand to retrieve these.
        /// </summary>
        [Contained]
        [JsonProperty(PropertyName = "assetGroups")]
        public IEnumerable<AssetGroup> AssetGroups { get; set; }

        /// <summary>
        /// The deployment location for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "deploymentLocation")]
        public string DeploymentLocation { get; set; }

        /// <summary>
        /// The data residency boundary for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "dataResidencyBoundary")]
        public string DataResidencyBoundary { get; set; }

        /// <summary>
        /// The cloud instances this agent supports.
        /// </summary>
        [JsonProperty(PropertyName = "supportedClouds")]
        public IEnumerable<string> SupportedClouds { get; set; }
    }
}