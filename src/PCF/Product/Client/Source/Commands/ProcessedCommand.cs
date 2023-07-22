namespace Microsoft.PrivacyServices.CommandFeed.Client.Commands
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;

    /// <summary>
    /// Defines a processed command ready to be checkpointed as completed.
    /// </summary>
    public class ProcessedCommand
    {
        /// <summary>
        /// The number of rows that were touched by this operation.
        /// </summary>
        public int AffectedRowCount { get; }

        /// <summary>
        /// The command Id.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// This property is set if an error occurs during a BulkComplete operation for this particular command.
        /// </summary>
        public string Error { get; internal set; }

        /// <summary>
        /// The lease receipt of the command.
        /// </summary>
        public string LeaseReceipt { get; internal set; }

        /// <summary>
        /// Auditor-friendly reasons for a permanent failure that prohibits the complete processing the command.
        /// </summary>
        public IEnumerable<string> NonTransientFailures { get; }

        /// <summary>
        /// The approved variantIds applicable to your agent.
        /// </summary>
        public IEnumerable<string> VariantIds { get; }

        /// <summary>
        /// Exported File Size Details
        /// </summary>
        public List<ExportedFileSizeDetails> ExportedFileSizeDetails { get; }

        /// <summary>
        /// The processed command ready to be checkpointed as completed.
        /// </summary>
        /// <param name="commandId">The command Id.</param>
        /// <param name="leaseReceipt">The lease receipt of the command.</param>
        /// <param name="affectedRowCount">The number of rows that were touched by this operation.</param>
        /// <param name="variantIds">The approved variantIds.</param>
        /// <param name="nonTransientFailures"> 
        /// Auditor-friendly reasons for a permanent failure that prohibits the complete processing the command.
        /// </param>
        /// <param name="exportedFileSizeDetails">Exported File size details</param>
        public ProcessedCommand(
            string commandId,
            string leaseReceipt,
            int affectedRowCount,
            IEnumerable<string> variantIds = null,
            IEnumerable<string> nonTransientFailures = null,
            List<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            this.CommandId = commandId;
            this.LeaseReceipt = leaseReceipt;
            this.AffectedRowCount = affectedRowCount;
            this.VariantIds = variantIds;
            this.NonTransientFailures = nonTransientFailures;
            this.ExportedFileSizeDetails = exportedFileSizeDetails;
        }

        /// <summary>
        /// Converts this parameter object to the internal <see cref="CheckpointCompleteRequest" /> object.
        /// </summary>
        internal static CheckpointCompleteRequest ToCheckpointCompleteRequest(ProcessedCommand processedCommand)
        {
            return new CheckpointCompleteRequest
            {
                CommandId = processedCommand.CommandId,
                LeaseReceipt = processedCommand.LeaseReceipt,
                RowCount = processedCommand.AffectedRowCount,
                NonTransientFailures = processedCommand.NonTransientFailures,
                VariantIds = processedCommand.VariantIds,
                ExportedFileSizeDetails = processedCommand.ExportedFileSizeDetails
            };
        }
    }
}
