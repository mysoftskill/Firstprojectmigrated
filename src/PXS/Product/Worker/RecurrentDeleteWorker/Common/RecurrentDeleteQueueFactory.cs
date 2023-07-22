namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using System;
    using global::Azure.Core;
    using global::Azure.Identity;

    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// RecurrentDeleteQueueFactory
    /// </summary>
    public class RecurrentDeleteQueueFactory : IRecurrentDeleteQueueFactory
    {
        private readonly ILogger logger;

        /// <summary>
        ///     Creates a new instance of CloudQueue />
        /// </summary>
        public RecurrentDeleteQueueFactory(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public ICloudQueue<RecurrentDeleteScheduleDbDocument> CreatePreVerifierQueue(IAzureStorageConfiguration queueConfig, string uami)
        {
            return CreateQueue(queueConfig, queueConfig.RefreshPreVerifierQueueName, uami);
        }

        /// <inheritdoc />
        public ICloudQueue<RecurrentDeleteScheduleDbDocument> CreateScheduleQueue(IAzureStorageConfiguration queueConfig, string uami)
        {
            return CreateQueue(queueConfig, queueConfig.RecurringDeleteQueueName, uami);
        }

        private ICloudQueue<RecurrentDeleteScheduleDbDocument> CreateQueue(IAzureStorageConfiguration queueConfig, string queueName, string uami)
        {
            string accountName = queueConfig.AccountName;
            bool useEmulator = queueConfig.UseEmulator;

            CloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue;
            if (useEmulator)
            {
                cloudQueue = new CloudQueue<RecurrentDeleteScheduleDbDocument>(queueName: queueName);
            }
            else
            {
                TokenCredential tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = uami });
                cloudQueue = new CloudQueue<RecurrentDeleteScheduleDbDocument>(accountName: accountName, queueName: queueName, credential: tokenCredential);
            }

            // create queue if not exists
            cloudQueue.CreateIfNotExistsAsync().Wait();

            return cloudQueue;
        }
    }
}
