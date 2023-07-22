namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Information about a specific data agent.
    /// </summary>
    public interface IDataAgentInfo
    {
        /// <summary>
        /// Gets the ID of this agent.
        /// </summary>
        AgentId AgentId { get; }

        /// <summary>
        /// Gets all of the asset group information that this agent supports.
        /// </summary>
        IEnumerable<IAssetGroupInfo> AssetGroupInfos { get; }

        /// <summary>
        /// Gets a value indicating whether this agent has ever queried for commands.
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// Tries to get the asset group with the given ID.
        /// </summary>
        bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo);

        /// <summary>
        /// Determines if this data agent matches the given MSA site ID.
        /// </summary>
        bool MatchesMsaSiteId(long msaSiteId);

        /// <summary>
        /// Determines if this data agent matches the given AAD App ID.
        /// </summary>
        bool MatchesAadAppId(Guid aadAppId);

        /// <summary>
        /// Marks this agent as being online and ready to receive commands.
        /// </summary>
        Task MarkAsOnlineAsync();

        /// <summary>
        /// Gets whether this data agent supports Cross Tenant
        /// </summary>
        bool IsOptedIntoAadSubject2();

        /// <summary>
        /// Gets whether this data agent is a PCF v2 batch agent
        /// </summary>
        bool IsV2Agent();
    }
}
