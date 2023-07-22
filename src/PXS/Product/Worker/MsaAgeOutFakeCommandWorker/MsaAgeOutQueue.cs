// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.MsaAgeOutFakeCommandWorker
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Microsoft.PrivacyServices.Common.Azure;

    public class MsaAgeOutQueue : IMsaAgeOutQueue
    {
        private const string QueueName = "test-msaageout";

        private readonly TimeSpan leaseDuration = TimeSpan.FromMinutes(15);

        private readonly ILogger logger;

        private readonly AzureQueue<AgeOutRequest> queue;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        public MsaAgeOutQueue(ILogger logger, IPrivacyConfigurationManager configurationManager)
        {
            this.logger = logger;
            IAzureStorageProvider storageProvider = new AzureStorageProvider(logger, new AzureKeyVaultReader(configurationManager, new Clock(), this.logger));
            storageProvider.InitializeAsync(configurationManager.MsaAgeOutFakeCommandWorkerConfiguration.QueueStorageConfiguration).GetAwaiter().GetResult();
            this.queue = new AzureQueue<AgeOutRequest>(storageProvider, this.logger, QueueName);
        }

        public Task<IList<IQueueItem<AgeOutRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken)
        {
            return this.queue.DequeueBatchAsync(this.leaseDuration, this.timeout, maxCount, new ExponentialRetry(), cancellationToken);
        }
    }
}
