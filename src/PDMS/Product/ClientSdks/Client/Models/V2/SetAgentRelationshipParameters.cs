[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// This object defines the payload for the V2.AssetGroups.SetAgentRelationship API.
    /// </summary>
    public class SetAgentRelationshipParameters
    {
        /// <summary>
        /// The set of available actions to apply to an asset group / agent relationship.
        /// </summary>
        public enum ActionType
        {
            /// <summary>
            /// Set the agent id for a specific capability on the asset group.
            /// </summary>
            Set = 1,

            /// <summary>
            /// Clear the agent id for a specific capability on the asset group.
            /// </summary>
            Clear = 2
        }

        /// <summary>
        /// The data owner that should be updated.
        /// It must have the serviceTree.serviceId set.        
        /// </summary>
        [JsonProperty(PropertyName = "relationships")]
        public IEnumerable<Relationship> Relationships { get; set; }
        
        /// <summary>
        /// Identifies the asset group to agent relationship changes for a specific asset group.
        /// </summary>
        public class Relationship
        {
            /// <summary>
            /// The id of the asset group for which the relationships apply.
            /// </summary>
            [JsonProperty(PropertyName = "assetGroupId")]
            public string AssetGroupId { get; set; }

            /// <summary>
            /// The ETag of the asset group for consistency checks.
            /// </summary>
            [JsonProperty(PropertyName = "eTag")]
            public string ETag { get; set; }

            /// <summary>
            /// The set of actions to apply to this asset group.
            /// </summary>
            [JsonProperty(PropertyName = "actions")]
            public IEnumerable<Action> Actions { get; set; }
        }

        /// <summary>
        /// Identifies a specific action to take regarding the relationship between an asset group and agent.
        /// </summary>
        public class Action
        {
            /// <summary>
            /// The type of action to perform.
            /// </summary>
            [JsonProperty(PropertyName = "verb")]
            public ActionType Verb { get; set; }

            /// <summary>
            /// The capability to modify.
            /// </summary>
            [JsonProperty(PropertyName = "capabilityId")]
            public CapabilityId CapabilityId { get; set; }

            /// <summary>
            /// The id of the agent that should be set.
            /// This should not be provided if clearing the agent id for a particular capability.
            /// </summary>
            [JsonProperty(PropertyName = "deleteAgentId")]
            public string DeleteAgentId { get; set; }
        }
    }
}