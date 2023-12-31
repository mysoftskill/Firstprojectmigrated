namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a single implementation of ICommandQueue that routes to all per-assetgroup queues for a given agent.
    /// </summary>
    /// <remarks>
    /// This class maintains many internal command queues for different asset groups for the given agent. The class uses
    /// lease receipts to route to the correct queue for all write commands. However, for pop, the behavior is somewhat different:
    /// 
    /// 1) The queues are organized into a linked list.
    /// 2) The list is rotated randomly, so that the same things are not always read in the same order.
    /// 3) The queues are polled in order, round robin. When polling, the next time to poll is set to (Now + 1 second).
    /// 4) When polling, if the next time to poll is in the future, we delay be the time difference to avoid overwhelming the database.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class DataAgentCommandQueue : RoundRobinCommandQueue
    {
        // Maps subject type -> asset group ID -> queue type -> queue. This is used for looking up queues based on lease receipt.
        private Dictionary<SubjectType, Dictionary<AssetGroupId, Dictionary<QueueStorageType, ICommandQueue>>> queueMap;

        private IList<QueueStorageType> supportedQueueStorageTypes = new List<QueueStorageType> { QueueStorageType.AzureCosmosDb, QueueStorageType.AzureQueueStorage};

        private const SubjectType SupportedSubjectAzureQueueStorage = SubjectType.Msa;

        public DataAgentCommandQueue(
            AgentId agentId,
            ICommandQueueFactory innerQueueFactory,
            IDataAgentMap dataAgentMap)
        {
            this.queueMap = new Dictionary<SubjectType, Dictionary<AssetGroupId, Dictionary<QueueStorageType, ICommandQueue>>>();
            
            dataAgentMap.TryGetAgent(agentId, out IDataAgentInfo agentInfo);

            var assetGroups = agentInfo?.AssetGroupInfos ?? new IAssetGroupInfo[0];

            foreach (IAssetGroupInfo assetGroupInfo in assetGroups)
            {
                if (assetGroupInfo?.SupportedSubjectTypes == null)
                {
                    // Nothing to do for this asset group.
                    continue;
                }

                foreach (SubjectType subjectType in Enum.GetValues(typeof(SubjectType)))
                {
                    if (!this.queueMap.ContainsKey(subjectType))
                    {
                        this.queueMap[subjectType] = new Dictionary<AssetGroupId, Dictionary<QueueStorageType, ICommandQueue>>();
                    }

                    foreach (QueueStorageType queueType in this.supportedQueueStorageTypes)
                    {
                        if (!this.queueMap[subjectType].ContainsKey(assetGroupInfo.AssetGroupId))
                        {
                            this.queueMap[subjectType][assetGroupInfo.AssetGroupId] = new Dictionary<QueueStorageType, ICommandQueue>();
                        }

                        switch (queueType)
                        {
                            case QueueStorageType.AzureCosmosDb:
                            case QueueStorageType.AzureQueueStorage when SupportedSubjectAzureQueueStorage == subjectType:
                            {
                                // Create queues for each agent / asset group / subject type / queue type combination using the provided factory.
                                ICommandQueue queue = innerQueueFactory.CreateQueue(agentId, assetGroupInfo.AssetGroupId, subjectType, queueType);
                                this.queueMap[subjectType][assetGroupInfo.AssetGroupId][queueType] = queue;
                                break;
                            }
                            
                            // other combinations don't add to queue map
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Be somewhat aggressive about checking subqueues here.
        /// </summary>
        protected override TimeSpan DelayBetweenItems => TimeSpan.FromMilliseconds(50);

        public override Task EnqueueAsync(string moniker, PrivacyCommand command)
        {
            Logger.Instance?.CommandsTransferred(1, command.AgentId.Value, command.AssetGroupId.Value, "DataAgentCommandQueueCommandInserted");
            return this.queueMap[command.Subject.GetSubjectType()][command.AssetGroupId][command.QueueStorageType].EnqueueAsync(moniker, command);
        }

        public override Task UpsertAsync(string moniker, PrivacyCommand command)
        {
            return this.queueMap[command.Subject.GetSubjectType()][command.AssetGroupId][command.QueueStorageType].UpsertAsync(moniker, command);
        }

        public override bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            return this.queueMap.ContainsKey(leaseReceipt.SubjectType) &&
                   this.queueMap[leaseReceipt.SubjectType].ContainsKey(leaseReceipt.AssetGroupId) &&
                   this.queueMap[leaseReceipt.SubjectType][leaseReceipt.AssetGroupId].ContainsKey(leaseReceipt.QueueStorageType) && 
                   this.queueMap[leaseReceipt.SubjectType][leaseReceipt.AssetGroupId][leaseReceipt.QueueStorageType].SupportsLeaseReceipt(leaseReceipt);
        }

        /// <summary>
        /// Indicates if the queue supports queue flush by date
        /// </summary>
        /// <returns>bool indicating if this is supported</returns>
        public override bool SupportsQueueFlushByDate => this.GetInnerQueues().All(c => c.SupportsQueueFlushByDate);

        public override Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations)
        {
            this.CheckLeaseReceipt(leaseReceipt);
            return this.queueMap[leaseReceipt.SubjectType][leaseReceipt.AssetGroupId][leaseReceipt.QueueStorageType].ReplaceAsync(leaseReceipt, command, commandReplaceOperations);
        }

        public override async Task DeleteAsync(LeaseReceipt leaseReceipt)
        {
            this.CheckLeaseReceipt(leaseReceipt);
            await this.queueMap[leaseReceipt.SubjectType][leaseReceipt.AssetGroupId][leaseReceipt.QueueStorageType].DeleteAsync(leaseReceipt);

            Logger.Instance?.CommandsTransferred(1, leaseReceipt.AgentId.Value, leaseReceipt.AssetGroupId.Value, "DataAgentCommandQueueCommandRemoved");
        }

        public override Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt)
        {
            this.CheckLeaseReceipt(leaseReceipt);
            this.SupportsQueryCommand(leaseReceipt);
            return this.queueMap[leaseReceipt.SubjectType][leaseReceipt.AssetGroupId][leaseReceipt.QueueStorageType].QueryCommandAsync(leaseReceipt);
        }

        public override async Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token)
        {
            foreach (var item in this.GetInnerQueues())
            {
                await item.AddQueueStatisticsAsync(resultBag, getDetailedStatistics, token);
            }
        }

        /// <summary>
        /// Call the AgentQueueFlush storedProc to delete all the commands in the agent queues
        /// </summary>
        /// <param name="flushDate">The command creation date of the latest command that needs to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Task</returns>
        public override async Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token)
        {
            var flushTasks = this.GetInnerQueues()
                .Where(x => x.SupportsQueueFlushByDate)
                .Select(x => x.FlushAgentQueueAsync(flushDate, token));

            await Task.WhenAll(flushTasks);
        }

        protected override IList<ICommandQueue> GetInnerQueues()
        {
            return this.queueMap.Values
                .SelectMany(x => x.Values)
                .SelectMany(y => y.Values)
                .ToList();
        }

        private void CheckLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            if (!this.SupportsLeaseReceipt(leaseReceipt))
            {
                throw new CommandFeedException("The given lease receipt was not supported.")
                {
                    ErrorCode = CommandFeedInternalErrorCode.InvalidLeaseReceipt,
                    IsExpected = true,
                };
            }
        }

        private void SupportsQueryCommand(LeaseReceipt leaseReceipt)
        {
            if (leaseReceipt.QueueStorageType == QueueStorageType.AzureQueueStorage)
            {
                throw new CommandFeedException("The command does not support Query Command.")
                {
                    ErrorCode = CommandFeedInternalErrorCode.NotSupported,
                    IsExpected = true,
                };
            }
        }
    }
}
