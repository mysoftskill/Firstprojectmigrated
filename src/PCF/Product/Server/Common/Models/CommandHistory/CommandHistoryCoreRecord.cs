namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    /// The core part of a command's cold storage data.
    /// </summary>
    public class CommandHistoryCoreRecord : ICommandHistoryChangeTrackedObject
    {
        // Backing fields for properties
        private DateTimeOffset createdTime;
        private long? totalCommandCount;
        private long? completedCommandCount;
        private long? ingestedCommandCount;
        private long? ingestionDataSetVersion;
        private string ingestionAssemblyVersion;
        private string requester;
        private string context;
        private bool isGloballyComplete;
        private DateTimeOffset? completedTime;
        private string rawPxsCommand;
        private IPrivacySubject subject;
        private PrivacyCommandType commandType;
        private bool isSynthetic;
        private Uri finalExportDestinationUri;
        private ExportArchivesDeleteStatus exportArchivesDeleteStatus;
        private DateTimeOffset? deleteRequestedTime;
        private DateTimeOffset? deletedTime;
        private string deleteRequester;
        private IReadOnlyList<string> weightedMonikerList;
        private QueueStorageType queueStorageType = QueueStorageType.AzureCosmosDb; // set as default

        /// <summary>
        /// Initializes a new CommandHistoryCoreRecord with the given command ID.
        /// </summary>
        public CommandHistoryCoreRecord(CommandId commandId)
        {
            this.CommandId = commandId;
            this.IsDirty = true;
        }

        /// <summary>
        /// The unique ID of the command.
        /// </summary>
        public CommandId CommandId { get; }

        /// <summary>
        /// The earliest time we have for this command being created.
        /// </summary>
        public DateTimeOffset CreatedTime
        {
            get => this.createdTime;
            set
            {
                this.createdTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The total number of commands that should be sent to different agents for processing.
        /// </summary>
        public long? TotalCommandCount
        {
            get => this.totalCommandCount;
            set
            {
                this.totalCommandCount = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The total number of commands that agents have acknowledged as completed.
        /// </summary>
        public long? CompletedCommandCount
        {
            get => this.completedCommandCount;
            set
            {
                this.completedCommandCount = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The total number of commands that have been ingested and inserted into queues.
        /// </summary>
        public long? IngestedCommandCount
        {
            get => this.ingestedCommandCount;
            set
            {
                this.ingestedCommandCount = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The data set version used to ingest this command.
        /// </summary>
        public long? IngestionDataSetVersion
        {
            get => this.ingestionDataSetVersion;
            set
            {
                this.ingestionDataSetVersion = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The assembly version used to ingest this command.
        /// </summary>
        public string IngestionAssemblyVersion
        {
            get => this.ingestionAssemblyVersion;
            set
            {
                this.ingestionAssemblyVersion = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The requester for the command, for example the dashboard site id or AAD tenant id.
        /// </summary>
        public string Requester
        {
            get => this.requester;
            set
            {
                this.requester = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The context to carry with the command, used to store ids in caller systems and so forth. Opaque to NGP.
        /// </summary>
        public string Context
        {
            get => this.context;
            set
            {
                this.context = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Indicates if the command has globally completed.
        /// </summary>
        public bool IsGloballyComplete
        {
            get => this.isGloballyComplete;
            set
            {
                this.isGloballyComplete = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// If the command is completed, when it completed.
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
        /// The raw command from PXS. Represented as a string so we can easily support many versions of the PXS contract.
        /// </summary>
        public string RawPxsCommand
        {
            get => this.rawPxsCommand;
            set
            {
                this.rawPxsCommand = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The subject of the command.
        /// </summary>
        public IPrivacySubject Subject
        {
            get => this.subject;
            set
            {
                this.subject = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The type of the command.
        /// </summary>
        public PrivacyCommandType CommandType
        {
            get => this.commandType;
            set
            {
                this.commandType = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not the command is a synthetic command for end to end testing.
        /// </summary>
        public bool IsSynthetic
        {
            get => this.isSynthetic;
            set
            {
                this.isSynthetic = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The final destination for an export command.
        /// Will be null if not an export command, or there is no final destination preference.
        /// </summary>
        public Uri FinalExportDestinationUri
        {
            get => this.finalExportDestinationUri;
            set
            {
                this.finalExportDestinationUri = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The field to check if an export command is already deleted by user before expiry.
        /// </summary>
        public ExportArchivesDeleteStatus ExportArchivesDeleteStatus
        {
            get => this.exportArchivesDeleteStatus;
            set
            {
                this.exportArchivesDeleteStatus = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The field to store requested time of delete request in case of on demand delete of the command
        /// </summary>
        public DateTimeOffset? DeleteRequestedTime
        {
            get => this.deleteRequestedTime;
            set
            {
                this.deleteRequestedTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The field to store actual deleted time of this export command in case of on demand delete
        /// </summary>
        public DateTimeOffset? DeletedTime
        {
            get => this.deletedTime;
            set
            {
                this.deletedTime = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The field to mention who initiated the delete request for this command in case of on demand delete
        /// </summary>
        public string DeleteRequester
        {
            get => this.deleteRequester;
            set
            {
                this.deleteRequester = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Contains a weighted list of monikers. This is stored in command history
        /// to make routing commands to agent queues completely deterministic. That is,
        /// a single command will always be routed to the same backing shard, because
        /// we are storing the moniker list immutably in one spot.
        /// </summary>
        public IReadOnlyList<string> WeightedMonikerList
        {
            get => this.weightedMonikerList;
            set
            {
                this.weightedMonikerList = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// The queue storage type this command is routed to for agent queues
        /// </summary>
        public QueueStorageType QueueStorageType
        {
            get => this.queueStorageType;
            set
            {
                this.queueStorageType = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Indicates if this object has modified.
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
