namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Helper utility for contextual interpretation of lease receipts.
    /// </summary>
    public static class LeaseReceiptUtility
    {
        /// <summary>
        /// Performs environment-specific handling of the lease reciept. This is largely a hook for the stress environment to "rewrite" its lease receipts.
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "commandQueue")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "commandHistory")]
        public static async Task<LeaseReceipt> LoadEnvironmentLeaseReceipt(
            LeaseReceipt leaseReceipt,
            ICommandHistoryRepository commandHistory,
            ICommandQueue commandQueue)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if INCLUDE_TEST_HOOKS
            if (Config.Instance.Common.IsStressEnvironment)
            {
                // Stress environments don't receive lease receipts targeted at their environment, since requests are just forwarded over from a different location.
                // So, we need to do a command history lookup to find the right value.
                var historyDocument = await commandHistory.QueryAsync(leaseReceipt.CommandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status);
                if (historyDocument?.Core == null)
                {
                    return leaseReceipt;
                }

                if (!historyDocument.StatusMap.TryGetValue((leaseReceipt.AgentId, leaseReceipt.AssetGroupId), out var statusRecord))
                {
                    return leaseReceipt;
                }

                var subjectType = historyDocument.Core.Subject.GetSubjectType();

                // Create a "fake" lease receipt, then use the queue's query method.
                LeaseReceipt virtualLeaseReceipt = new LeaseReceipt(
                    databaseMoniker: statusRecord.StorageAccountMoniker,
                    commandId: leaseReceipt.CommandId,
                    token: string.Empty,   // etag is null since we don't have one.
                    assetGroupId: leaseReceipt.AssetGroupId,
                    agentId: leaseReceipt.AgentId,
                    subjectType: subjectType,
                    approximateExpirationTime: DateTimeOffset.MinValue,
                    assetGroupQualifier: string.Empty,
                    commandType: historyDocument.Core.CommandType,
                    cloudInstance: string.Empty,
                    commandCreatedTime: leaseReceipt.CommandCreatedTime,
                    queueStorageType: QueueStorageType.AzureCosmosDb);

                var command = await commandQueue.QueryCommandAsync(virtualLeaseReceipt);
                if (command == null)
                {
                    return leaseReceipt;
                }

                return command.LeaseReceipt;
            }
#endif

            return leaseReceipt;
        }
    }
}
