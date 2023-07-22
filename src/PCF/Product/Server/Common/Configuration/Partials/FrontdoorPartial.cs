namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System.Collections.Generic;

    public partial class Configuration_Frontdoor
    {
        private HashSet<AgentId> syntheticAgentIds;

        partial void OnInitialized()
        {
            HashSet<AgentId> agentIds = new HashSet<AgentId>();
            foreach (var item in this.SyntheticAgents)
            {
                agentIds.Add(new AgentId(item));
            }

            this.syntheticAgentIds = agentIds;
        }

        /// <summary>
        /// Check if it is a synthetic test agent
        /// </summary>
        public bool IsSyntheticTestAgent(AgentId agentId)
        {
            return this.syntheticAgentIds.Contains(agentId);
        }
    }
}
