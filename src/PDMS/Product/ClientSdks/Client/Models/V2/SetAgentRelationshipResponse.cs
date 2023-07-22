[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// This class represents the response payload for the V2.AssetGroups.SetAgentRelationship API.
    /// </summary>
    public class SetAgentRelationshipResponse
    {
        /// <summary>
        /// The possible actions that can be applied to the asset group.
        /// </summary>
        public enum StatusType
        {
            /// <summary>
            /// Indicates that the asset group was updated
            /// for the specified capability link.
            /// </summary>
            Updated = 1,

            /// <summary>
            /// Indicates that the asset group was added to a request
            /// for the specified capability link.
            /// </summary>
            Requested = 2,

            /// <summary>
            /// Indicates that the capability link or pending link request was removed.
            /// </summary>
            Removed = 3,
        }

        /// <summary>
        /// The results from the API. There will be a separate entry for each asset group id in the original request.
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public IEnumerable<AssetGroupResult> Results { get; set; }

        /// <summary>
        /// The result payload for an individual asset group.
        /// </summary>
        public class AssetGroupResult
        {
            /// <summary>
            /// The id of the asset group.
            /// </summary>
            [JsonProperty(PropertyName = "assetGroupId")]
            public string AssetGroupId { get; set; }

            /// <summary>
            /// The ETag of the asset group. 
            /// This is necessary for subsequent updates on the asset group 
            /// if the caller does not want to re-read the entire asset group.
            /// </summary>
            [JsonProperty(PropertyName = "eTag")]
            public string ETag { get; set; }

            /// <summary>
            /// The actions that were performed for the asset group capability links.
            /// </summary>
            [JsonProperty(PropertyName = "capabilities")]
            public IEnumerable<CapabilityResult> Capabilities { get; set; }
        }

        /// <summary>
        /// Identifies the result for a specific capability link.
        /// </summary>
        public class CapabilityResult
        {
            /// <summary>
            /// The capability specified in this link.
            /// </summary>
            [JsonProperty(PropertyName = "capabilityId")]
            public CapabilityId CapabilityId { get; set; }

            /// <summary>
            /// The action that was performed for the 
            /// specific capability link on this asset group.
            /// </summary>
            [JsonProperty(PropertyName = "status")]
            public StatusType Status { get; set; }

            /// <summary>
            /// The id of the sharing request for this specific capability.
            /// This will be null if the capability was updated directly.
            /// </summary>
            [JsonProperty(PropertyName = "sharingRequestId")]
            public string SharingRequestId { get; set; }
        }
    }
}