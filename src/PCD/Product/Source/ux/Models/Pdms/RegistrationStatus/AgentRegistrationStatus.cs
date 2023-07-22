using System.Collections.Generic;
using Microsoft.PrivacyServices.Policy;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms.RegistrationStatus
{
    /// <summary>
    /// Agent registration status.
    /// </summary>
    public class AgentRegistrationStatus
    {
        /// <summary>
        /// The id of the agent.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The owner id of the agent.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// An overall summary of whether or not the agent registration is complete.
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// The set of protocols for this agent.
        /// </summary>
        public IEnumerable<ProtocolId> Protocols { get; set; }
        
        /// <summary>
        /// The protocol registration status.
        /// </summary>
        public PdmsApiModelsV2.RegistrationState ProtocolStatus { get; set; }

        /// <summary>
        /// The set of environments that the agent connection details target. 
        /// </summary>
        public IEnumerable<PdmsApiModelsV2.ReleaseState> Environments { get; set; }

        /// <summary>
        /// The environment registration status.
        /// </summary>
        public PdmsApiModelsV2.RegistrationState EnvironmentStatus { get; set; }

        /// <summary>
        /// The set of capabilities for this agent.
        /// </summary>
        public IEnumerable<CapabilityId> Capabilities { get; set; }

        /// <summary>
        /// The capability registration status. 
        /// </summary>
        public PdmsApiModelsV2.RegistrationState CapabilityStatus { get; set; }

        /// <summary>
        /// The set of asset group registration statuses for all asset groups linked to this agent.
        /// </summary>
        public IEnumerable<AssetGroupRegistrationStatus> AssetGroups { get; set; }

        /// <summary>
        /// An overall summary of whether or not all asset group registrations are correct. 
        /// </summary>
        public PdmsApiModelsV2.RegistrationState AssetGroupsStatus { get; set; }
    }
}