namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Enumerates possible actions taken when PCF receives a command.
    /// </summary>
    public enum CommandIngestionStatus
    {
        /// <summary>
        /// Unclear what happened. Used as a default value.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The command was dropped because we didn't recognize the agent ID.
        /// </summary>
        [Obsolete("No longer possible")]
        DroppedDueToUnknownAgentId = 1,

        /// <summary>
        /// The command was dropped because we didn't recognize the asset group ID.
        /// </summary>
        [Obsolete("No longer possible")]
        DroppedDueToUnknownAssetGroupId = 2,
        
        /// <summary>
        /// The command was determined to not be applicable to this asset group.
        /// </summary>
        DroppedDueToFiltering = 3,

        /// <summary>
        /// The command was inserted into the agent's queue.
        /// </summary>
        SentToAgent = 4,

        /// <summary>
        /// The command is being sent into the agent's queue. This state means that the command *may* be in the agent's queue.
        /// </summary>
        SendingToAgent = 5,
       
        /// <summary>
        /// The AssetGroup has a variant that applies to this command, so marking it as complete and not sending to agent
        /// </summary>
        DroppedByApplyingVariant = 6,

        /// <summary>
        /// The command matched the tagging, but was dropped because the agent was not online.
        /// </summary>
        DroppedDueToOfflineAgent = 7
    }

    /// <summary>
    /// Defines the ingestion status for a command for a particular asset group.
    /// </summary>
    public class CommandIngestionAuditRecord : ICommandHistoryChangeTrackedObject
    {
        private CommandIngestionStatus? ingestionStatus;
        private ApplicabilityReasonCode? matchError;
        private string debugText;
        
        /// <summary>
        /// Indicates what action PCF took when ingesting this command.
        /// </summary>
        [JsonProperty("is")]
        [JsonConverter(typeof(StringEnumConverter))]
        public CommandIngestionStatus? IngestionStatus
        {
            get => this.ingestionStatus;
            set
            {
                this.ingestionStatus = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Any error we received when matching the command against the PDMS data.
        /// </summary>
        [JsonProperty("me")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ApplicabilityReasonCode? ApplicabilityReasonCode
        {
            get => this.matchError;
            set
            {
                this.matchError = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// A human-readable error message about what PCF did with this command.
        /// </summary>
        [JsonProperty("dt")]
        [JsonConverter(typeof(InternedStringJsonConverter))]
        public string DebugText
        {
            get => this.debugText;
            set
            {
                this.debugText = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Indicates if the current object has been modified.
        /// </summary>
        [JsonIgnore]
        public bool IsDirty { get; private set; } = true;

        /// <summary>
        /// Clears the dirty flag, if necessary.
        /// </summary>
        public void ClearDirty()
        {
            this.IsDirty = false;
        }
    }
}
