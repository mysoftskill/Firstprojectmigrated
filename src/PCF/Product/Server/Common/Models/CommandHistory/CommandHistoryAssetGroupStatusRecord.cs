namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the status of a command for a single agent/asset group pair in cold storage.
    /// </summary>
    public class CommandHistoryAssetGroupStatusRecord : ICommandHistoryChangeTrackedObject
    {
        private DateTimeOffset? ingestionTime;
        private DateTimeOffset? softDeleteTime;
        private DateTimeOffset? completedTime;
        private bool? delinked;
        private IReadOnlyList<string> claimedVariants;
        private bool forceCompleted;
        private int? affectedRows;
        private string storageAccountMoniker;

        /// <summary>
        /// Initializes a new CommandHistoryAssetGroupStatusRecord with the given command ID.
        /// </summary>
        public CommandHistoryAssetGroupStatusRecord(AgentId agentId, AssetGroupId assetGroupId)
        {
            this.AgentId = agentId;
            this.AssetGroupId = assetGroupId;
            this.IsDirty = true;
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
        /// The time at which the command was ingested into this asset group's queue.
        /// </summary>
        public DateTimeOffset? IngestionTime
        {
            get => this.ingestionTime;
            set
            {
                this.ingestionTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The time at which the agent report non use for this asset group.
        /// </summary>
        public DateTimeOffset? SoftDeleteTime
        {
            get => this.softDeleteTime;
            set
            {
                this.softDeleteTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The time at which the agent reported completion for this asset group.
        /// </summary>
        public DateTimeOffset? CompletedTime
        {
            get => this.completedTime;
            set
            {
                this.completedTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The number of rows affected for this asset group.
        /// </summary>
        public int? AffectedRows
        {
            get => this.affectedRows;
            set
            {
                this.affectedRows = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// True if the agent delinked the data.
        /// </summary>
        public bool? Delinked
        {
            get => this.delinked;
            set
            {
                this.delinked = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Any variants claimed by the agent for this asset group.
        /// </summary>
        public IReadOnlyList<string> ClaimedVariants
        {
            get => this.claimedVariants;
            set
            {
                this.claimedVariants = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Indicates if the command was force completed for this asset group.
        /// </summary>
        public bool ForceCompleted
        {
            get => this.forceCompleted;
            set
            {
                this.forceCompleted = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The storage account moniker that hosts this command.
        /// </summary>
        public string StorageAccountMoniker
        {
            get => this.storageAccountMoniker;
            set
            {
                this.storageAccountMoniker = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Gets a value indicating if this object has changed.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Clears the dirty flag.
        /// </summary>
        public void ClearDirty()
        {
            this.IsDirty = false;
        }
    }
}
