namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System;
    using System.Collections.Generic;

    public partial class Configuration_CosmosDBQueues
    {
        private Dictionary<AgentId, TimeSpan> leaseDurationOverrides;

        partial void OnInitialized()
        {
            Dictionary<AgentId, TimeSpan> overrides = new Dictionary<AgentId, TimeSpan>();
            foreach (var item in this.DefaultLeaseOverrides)
            {
                overrides[new AgentId(item.AgentId)] = TimeSpan.FromSeconds(item.LeaseDurationSeconds);
            }

            this.leaseDurationOverrides = overrides;
        }

        /// <summary>
        /// Gets the lease duration for the given agent ID.
        /// </summary>
        public TimeSpan GetLeaseDuration(AgentId agentId)
        {
            if (this.leaseDurationOverrides.TryGetValue(agentId, out TimeSpan value))
            {
                return value;
            }

            return TimeSpan.FromSeconds(this.DefaultLeaseDurationSeconds);
        }
    }
}
