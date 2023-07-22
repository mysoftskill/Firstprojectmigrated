namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// A callback that can be set on a JsonCommand to handle a checkpoint operation.
    /// </summary>
    public delegate Task<string> CheckpointCallback(
            string commandId,
            string agentState,
            CommandStatus commandStatus,
            int affectedRowCount,
            string leaseReceipt,
            TimeSpan? leaseExtension,
            IEnumerable<string> variantIds,
            IEnumerable<string> nonTransientFailures,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null);

    // Adds extra client side methods to the PrivacyCommand class.
    public abstract partial class PrivacyCommand : IPrivacyCommand
    {
        /// <summary>
        /// A protected constuctor for a privacy command.
        /// </summary>
        internal PrivacyCommand(
            string commandType,
            string commandId,
            string assetGroupId,
            string assetGroupQualifier,
            string verifier,
            string correlationVector,
            string leaseReceipt,
            DateTimeOffset approximateLeaseExpiration,
            DateTimeOffset createdTime,
            IPrivacySubject subject,
            string agentState,
            ICommandFeedClient commandFeedClient,
            string cloudInstance) : this(commandType)
        {
            this.CommandId = commandId;
            this.AssetGroupId = assetGroupId;
            this.AssetGroupQualifier = assetGroupQualifier;
            this.Verifier = verifier;
            this.CorrelationVector = correlationVector;
            this.LeaseReceipt = leaseReceipt;
            this.ApproximateLeaseExpiration = approximateLeaseExpiration;
            this.Timestamp = createdTime;
            this.Subject = subject;
            this.AgentState = agentState;
            this.CloudInstance = cloudInstance;

            if (string.IsNullOrWhiteSpace(cloudInstance))
            {
                this.CloudInstance = Policies.Current.CloudInstances.Ids.Public.Value;
            }
            else
            {
                this.CloudInstance = cloudInstance;
            }

            if (commandFeedClient != null)
            {
                this.CheckpointCallback = commandFeedClient.CheckpointAsync;
            }
        }

        /// <summary>
        /// Set a callback that will be invoked when <see cref="CheckpointAsync(CommandStatus, int, TimeSpan?, IEnumerable{string}, IEnumerable{string}, IEnumerable{ExportedFileSizeDetails})"/> is called.
        /// </summary>
        [JsonIgnore]
        public CheckpointCallback CheckpointCallback { get; set; }

        /// <inheritdoc/>
        public async Task CheckpointAsync(
            CommandStatus commandStatus, 
            int affectedRowCount, 
            TimeSpan? leaseExtension = null, 
            IEnumerable<string> claimedVariants = null, 
            IEnumerable<string> nonTransientFailures = null,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            CheckpointCallback callback = this.CheckpointCallback;
            if (callback != null)
            {
                string newLeaseReceipt = await callback(
                    this.CommandId,
                    this.AgentState,
                    commandStatus,
                    affectedRowCount,
                    this.LeaseReceipt,
                    leaseExtension,
                    claimedVariants,
                    nonTransientFailures,
                    exportedFileSizeDetails).ConfigureAwait(false);

                this.LeaseReceipt = newLeaseReceipt;

                if (leaseExtension != null)
                {
                    this.ApproximateLeaseExpiration += leaseExtension.Value;
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(this.CheckpointCallback), "Please ensure that the CheckpointCallback is not null.");
            }
        }
    }
}
