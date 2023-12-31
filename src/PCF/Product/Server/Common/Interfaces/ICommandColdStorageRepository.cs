namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Operation flags.
    /// </summary>
    [Flags]
    public enum CommandHistoryFragmentTypes : uint
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The details about the core document should be populated.
        /// </summary>
        Core = 1,

        /// <summary>
        /// The details about the command status should be populated.
        /// </summary>
        Status = 2,

        /// <summary>
        /// The details about the audit should be populated.
        /// </summary>
        Audit = 4,

        /// <summary>
        /// The export destinations should be populated.
        /// </summary>
        ExportDestinations = 8,

        /// <summary>
        /// A union of all operation types.
        /// </summary>
        All = Core | Status | Audit | ExportDestinations,
    }

    /// <summary>
    /// An interface that provides insight into command "cold storage". Cold Storage is the store of record for the following 
    /// information:
    ///  - Overall command progress (which agents have finished, which are processing)
    ///  - Raw command information ingested from PXS. Used for keeping historical artifacts.
    /// </summary>
    public interface ICommandHistoryRepository
    {
        /// <summary>
        /// Queries information about the given command according to the given flags.
        /// </summary>
        Task<CommandHistoryRecord> QueryAsync(
            CommandId commandId,
            CommandHistoryFragmentTypes fragmentsToRead);

        /// <summary>
        /// Queries for commands issued by particular where clauses.
        /// </summary>
        Task<IEnumerable<CommandHistoryRecord>> QueryAsync(
            IPrivacySubject subject,
            string requester,
            IList<PrivacyCommandType> commandTypes,
            DateTimeOffset oldestRecord,
            CommandHistoryFragmentTypes fragmentsToRead);

        /// <summary>
        /// Queries for partially ingested commands between the two timestamps.
        /// </summary>
        Task<(IEnumerable<CommandHistoryRecord> records, string nextContinuationToken)> QueryPartiallyIngestedCommandsAsync(
            DateTimeOffset oldestRecordTimestamp,
            DateTimeOffset newestRecordTimestamp,
            int maxItemCount,
            bool exportOnly = false,
            bool nonExportOnly = false,
            string continuationToken = null);

        /// <summary>
        /// Queries for incomplete export commands between the two timestamps.
        /// </summary>
        Task<IEnumerable<CommandHistoryRecord>> QueryIncompleteExportsAsync(
            DateTimeOffset oldestRecordTimestamp,
            DateTimeOffset newestRecordTimestamp,
            bool aadOnly,
            CommandHistoryFragmentTypes fragmentsToRead);

        /// <summary>
        /// Get a list of raw pxscommands for replay purpose from cold storage
        /// </summary>
        Task<(IEnumerable<JObject> pxsCommands, string continuationToken)> GetCommandsForReplayAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string subjectType,
            bool includeExportCommands,
            string continuationToken,
            int maxItemCount = 1000);

        /// <summary>
        /// Does an etag-safe replace of the document.
        /// </summary>
        Task ReplaceAsync(
            CommandHistoryRecord record,
            CommandHistoryFragmentTypes fragmentsToModify);

        /// <summary>
        /// Inserts the given record into Cold Storage.
        /// </summary>
        /// <returns>True if the document has already been inserted.</returns>
        Task<bool> TryInsertAsync(CommandHistoryRecord record);

        /// <summary>
        /// Queries for the command matching the leaseReceipt, and returns a <see cref="PrivacyCommand"/>
        /// </summary>
        /// <param name="leaseReceipt">The lease receipt</param>
        /// <returns></returns>
        Task<PrivacyCommand> QueryPrivacyCommandAsync(LeaseReceipt leaseReceipt);

        /// <summary>
        /// Queries for the command to see if it is complete matching the leaseReceipt
        /// </summary>
        /// <param name="leaseReceipt"></param>
        /// <returns>bool indicating if the command is complete</returns>
        Task<bool> QueryIsCompleteByAgentAsync(LeaseReceipt leaseReceipt);
    }
}