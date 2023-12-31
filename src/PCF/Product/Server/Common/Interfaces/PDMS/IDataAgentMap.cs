namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The data agent map (PDMS).
    /// </summary>
    public interface IDataAgentMap
    {
        /// <summary>
        /// Gets the Data Agent Info for the given Agent ID.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
        IDataAgentInfo this[AgentId agentId] { get; }

        /// <summary>
        /// The version of the data backing this map.
        /// </summary>
        long Version { get; }

        /// <summary>
        /// The cosmos stream from which the AssetGroupCollection is loaded from
        /// </summary>
        string AssetGroupInfoStreamName { get; }

        /// <summary>
        /// The cosmos stream from which the AssetGroupVariantInfo is loaded from
        /// </summary>
        string VariantInfoStreamName { get; }

        /// <summary>
        /// Attempts to check for an agent with the given ID. Returns true if found and false otherwise.
        /// </summary>
        bool TryGetAgent(AgentId agentId, out IDataAgentInfo dataAgentInfo);

        /// <summary>
        /// Gets a collection of agent IDs present in this map.
        /// </summary>
        IEnumerable<AgentId> GetAgentIds();

        /// <summary>
        /// Gets a collection of AssetGroupInfos
        /// </summary>
        IEnumerable<IAssetGroupInfo> AssetGroupInfos { get; }
    }
}
