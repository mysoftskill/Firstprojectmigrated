namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// There is one CommandHistoryExportDestinationRecord per agent/assetgroup pair, keeping track of the staging storage.
    /// </summary>
    /// <remarks>
    /// This object is currently immutable. If that changes, implementors will need to properly implement the ICommandHistoryChangeTrackedObject interface.
    /// </remarks>
    public class CommandHistoryExportDestinationRecord : ICommandHistoryChangeTrackedObject
    {
        /// <summary>
        /// Initializes a new CommandHistoryExportDestinationRecord with the given data.
        /// </summary>
        public CommandHistoryExportDestinationRecord(AgentId agentId, AssetGroupId assetGroupId, Uri exportDestinationUri, string exportDestinationPath)
        {
            this.AgentId = agentId;
            this.AssetGroupId = assetGroupId;
            this.ExportDestinationUri = exportDestinationUri;
            this.ExportDestinationPath = exportDestinationPath;
        }

        /// <summary>
        /// The ID of the agent.
        /// </summary>
        public AgentId AgentId { get; }

        /// <summary>
        /// The ID of the asset group.
        /// </summary>
        public AssetGroupId AssetGroupId { get; }

        /// <summary>
        /// The staging destination for export.
        /// </summary>
        public Uri ExportDestinationUri { get; }

        /// <summary>
        /// The staging destination path for export.
        /// </summary>
        public string ExportDestinationPath { get; }

        /// <summary>
        /// This object is immutable, so is never dirty.
        /// </summary>
        public bool IsDirty => false;

        /// <summary>
        /// Nothing to clear.
        /// </summary>
        public void ClearDirty()
        {
        }
    }
}
