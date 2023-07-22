namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A list of commands and target destinations that needs to be enqueued for the replay request.
    /// </summary>
    public class ReplayCommandDestinationPair
    {
        public ReplayCommandDestinationPair(JObject rawPxsCommand, List<(AgentId agentId, AssetGroupId assetGroupId)> agentQueueTargets)
        {
            this.RawPxsCommand = rawPxsCommand;
            this.AgentQueueTargets = agentQueueTargets;
        }

        /// <summary>
        /// raw PXS command
        /// </summary>
        public JObject RawPxsCommand { get; set; }

        /// <summary>
        /// List of AgentId/AssetGroupId tuples for the enqueue destination
        /// </summary>
        public List<(AgentId agentId, AssetGroupId assetGroupId)> AgentQueueTargets { get; set; }
    }
}
