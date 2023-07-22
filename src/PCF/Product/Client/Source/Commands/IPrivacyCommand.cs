namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// Defines an interface representing a generic privacy command.
    /// </summary>
    public interface IPrivacyCommand
    {
        /// <summary>
        /// The approximate time that the lease for this command will expire.
        /// </summary>
        DateTimeOffset ApproximateLeaseExpiration { get; }

        /// <summary>
        /// The ID of the asset group that this command targets.
        /// </summary>
        string AssetGroupId { get; }

        /// <summary>
        /// The Qualifier of the asset group that this command targets.
        /// </summary>
        string AssetGroupQualifier { get; }

        /// <summary>
        /// The PCF command verifier.
        /// </summary>
        string Verifier { get; }

        /// <summary>
        /// The unique ID of the command.
        /// </summary>
        string CommandId { get; }

        /// <summary>
        /// The ID of the batch that this command is a part of. This ID is not unique.
        /// </summary>
        string RequestBatchId { get; set; }

        /// <summary>
        /// The incremental state of this command. The value in this property will be saved when CheckpointAsync is invoked.
        /// </summary>
        string AgentState { get; set; }

        /// <summary>
        /// The correlation vector.
        /// </summary>
        string CorrelationVector { get; }

        /// <summary>
        /// The duration of the lease, from when the command was received.
        /// </summary>
        TimeSpan LeaseDuration { get; }

        /// <summary>
        /// The lease receipt of the command.
        /// </summary>
        string LeaseReceipt { get; }

        /// <summary>
        /// The subject of the command. This will be a <see cref="MsaSubject"/>, <see cref="AadSubject"/>, <see cref="AadSubject2"/>, <see cref="DeviceSubject"/>, or -- less commonly -- a <see cref="DemographicSubject"/>.
        /// </summary>
        IPrivacySubject Subject { get; }

        /// <summary>
        /// The time at which the command was issued.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Approved variants that can be applied by the agent.
        /// </summary>
        IList<Variant> ApplicableVariants { get; set; }

        /// <summary>
        /// The public or sovereign cloud instance target for this command.
        /// </summary>
        string CloudInstance { get; set; }

        /// <summary>
        /// Indicates whether this command is scoped to processor data.
        /// </summary>
        bool ProcessorApplicable { get; set; }

        /// <summary>
        /// Indicates whether this command is scoped to controller data.
        /// </summary>
        bool ControllerApplicable { get; set; }

        /// <summary>
        /// Updates the incremental progress of this command.
        /// </summary>
        /// <param name="commandStatus">The status of the command.</param>
        /// <param name="affectedRowCount">The number of rows that were touched by this operation.</param>
        /// <param name="leaseExtension">A lease extension, if desired.</param>
        /// <param name="claimedVariants">The set of variants claimed by this checkpoint operation.</param>
        /// <param name="nonTransientFailures">The set of failures represent conditions that are not retryable for the command.</param>
        /// <param name="exportedFileSizeDetails">Export file size details list.</param>
        /// <returns>A task for completion.</returns>
        Task CheckpointAsync(
            CommandStatus commandStatus,
            int affectedRowCount,
            TimeSpan? leaseExtension = null,
            IEnumerable<string> claimedVariants = null,
            IEnumerable<string> nonTransientFailures = null,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null);
    }
}