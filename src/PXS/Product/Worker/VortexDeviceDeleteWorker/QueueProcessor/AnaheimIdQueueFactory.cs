namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    public class AnaheimIdQueueFactory : IAnaheimIdQueueFactory
    {
        private readonly ILogger logger;

        public AnaheimIdQueueFactory(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
        }

        public ICloudQueue<AnaheimIdRequest> Create(IAzureStorageConfiguration queueConfig)
        {            
            string accountName = queueConfig.AccountName;
            string queueName = queueConfig.AnaheimIdQueueName;
            string uamiId = queueConfig.AnaheimIdUamiId;
            bool useEmulator = queueConfig.UseEmulator;

            this.logger.Information(nameof(AnaheimIdQueueFactory), $"Create cloud queue: accountName={accountName}, queueName={queueName}, uamiId={uamiId}, useEmulator={useEmulator}");
            CloudQueue<AnaheimIdRequest> cloudQueue;
            if (useEmulator)
            {
                cloudQueue = new CloudQueue<AnaheimIdRequest>(queueName: queueName);
            }
            else
            {
                TokenCredential tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = uamiId });
                cloudQueue = new CloudQueue<AnaheimIdRequest>(accountName: accountName, queueName: queueName, credential: tokenCredential);
            }

            // create queue if not exists
            cloudQueue.CreateIfNotExistsAsync().Wait();

            return cloudQueue;
        }
    }
}
