// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    public class AssetGroupAzureQueueTrackerCache : IAssetGroupAzureQueueTrackerCache
    {
        private readonly ConcurrentDictionary<(string accountName, string queueName), QueueTracker> queueTrackerDictionary = new ConcurrentDictionary<(string, string), QueueTracker>();

        /// <inheritdoc />
        public bool QueueExists(IAzureCloudQueue cloudQueue)
        {
            if (this.queueTrackerDictionary.TryGetValue((cloudQueue.AccountName, cloudQueue.QueueName), out QueueTracker queueTracker))
            {
                return queueTracker.Exists;
            }

            return false;
        }

        /// <inheritdoc />
        public void StartQueueTracker(IAzureCloudQueue cloudQueue, PrivacyCommandType commandType)
        {
            if (this.queueTrackerDictionary.TryGetValue((cloudQueue.AccountName, cloudQueue.QueueName), out QueueTracker _))
            {
                // no need to start when it already exists
                return;
            }

            this.queueTrackerDictionary.TryAdd((cloudQueue.AccountName, cloudQueue.QueueName), new QueueTracker(cloudQueue, commandType));
        }

        private class QueueTracker
        {
            private readonly int maxQueueCheckDelayMilliseconds = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            public bool Exists => this.existValue == 1;

            private int existValue = 0;

            private Task queueTrackerTask;

            public QueueTracker(IAzureCloudQueue cloudQueue, PrivacyCommandType commandType)
            {
                if (commandType != PrivacyCommandType.AgeOut)
                {
                    // for performance reasons, not tracking queues for other command types
                    return;
                }

                this.queueTrackerTask = Task.Run(async () =>
                {
                    do
                    {
                        if (!this.Exists)
                        {
                            await Logger.InstrumentAsync(
                                new OutgoingEvent(SourceLocation.Here()),
                                async ev =>
                                {
                                    bool value = await cloudQueue.ExistsAsync(CancellationToken.None).ConfigureAwait(false);
                                    ev["QueueExists"] = value.ToString();
                                    ev["AccountName"] = cloudQueue.AccountName;
                                    ev["QueueName"] = cloudQueue.QueueName;

                                    // use interlocked exchange for thread safety (which doesn't work on bool)
                                    Interlocked.Exchange(ref this.existValue, value ? 1 : 0);
                                });

                            if (this.Exists)
                            {
                                DualLogger.Instance.Verbose(nameof(AssetGroupAzureQueueTrackerCache), $"{cloudQueue.QueueName} in {cloudQueue.AccountName} Exist...");
                                // no need to keep task running if queue exists
                                return Task.CompletedTask;
                            }
                        }
                        else
                        {
                            return Task.CompletedTask;
                        }

                        // use random delay to prevent multiple queue tracker task from happening at the same time
                        int delay = RandomHelper.Next(this.maxQueueCheckDelayMilliseconds / 2, this.maxQueueCheckDelayMilliseconds);
                        await Task.Delay(delay).ConfigureAwait(false);
                    } while (true);
                });
            }
        }
    }
}
