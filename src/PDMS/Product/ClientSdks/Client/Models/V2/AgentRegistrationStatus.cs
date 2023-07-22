[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]
namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// The agent registration status.
    /// </summary>
    public class AgentRegistrationStatus
    {
        /// <summary>
        /// The id of the agent.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The owner id of the agent.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// An overall summary of whether or not the agent registration is complete.
        /// </summary>
        [JsonProperty(PropertyName = "isComplete")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// The set of protocols for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "protocols")]
        public IEnumerable<ProtocolId> Protocols { get; set; }

        /// <summary>
        /// The protocol registration status.
        /// </summary>
        [JsonProperty(PropertyName = "protocolStatus")]
        public RegistrationState ProtocolStatus { get; set; }

        /// <summary>
        /// The set of environments that the agent connection details target.
        /// </summary>
        [JsonProperty(PropertyName = "environments")]
        public IEnumerable<ReleaseState> Environments { get; set; }

        /// <summary>
        /// The environment registration status.
        /// </summary>
        [JsonProperty(PropertyName = "environmentStatus")]
        public RegistrationState EnvironmentStatus { get; set; }

        /// <summary>
        /// The set of capabilities for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<CapabilityId> Capabilities { get; set; }

        /// <summary>
        /// The capability registration status.
        /// </summary>
        [JsonProperty(PropertyName = "capabilityStatus")]
        public RegistrationState CapabilityStatus { get; set; }

        /// <summary>
        /// The set of asset group registration statuses for all asset groups linked to this agent.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroups")]
        public IEnumerable<AssetGroupRegistrationStatus> AssetGroups { get; set; }

        /// <summary>
        /// An overall summary of whether or not all asset group registrations are correct.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroupsStatus")]
        public RegistrationState AssetGroupsStatus { get; set; }
    }
}