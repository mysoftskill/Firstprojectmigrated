namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.Common.Azure;


    public class AnaheimIdQueueWorkerFactory : IAnaheimIdQueueWorkerFactory
    {
        private readonly IPcfAdapter pcfAdapter;
        private readonly IAnaheimIdQueueFactory anaheimIdQueueFactory;
        private readonly IPrivacyConfigurationManager configuration;
        private readonly IAppConfiguration appConfiguration;
        private readonly ILogger logger;

        public AnaheimIdQueueWorkerFactory(
            IPcfAdapter pcfAdapter,
            IAnaheimIdQueueFactory anaheimIdQueueFactory,
            IPrivacyConfigurationManager configuration,
            IAppConfiguration appConfiguration,
            ILogger logger)
        {
            if (pcfAdapter is null)
            {
                throw new ArgumentNullException(nameof(pcfAdapter));
            }

            if (anaheimIdQueueFactory is null)
            {
                throw new ArgumentNullException(nameof(anaheimIdQueueFactory));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (appConfiguration is null)
            {
                throw new ArgumentNullException(nameof(appConfiguration));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.pcfAdapter = pcfAdapter;
            this.anaheimIdQueueFactory = anaheimIdQueueFactory;
            this.configuration = configuration;
            this.appConfiguration = appConfiguration;
            this.logger = logger;
        }

        public IWorker Create(IAzureStorageConfiguration queueConfig)
        {
            var cloudQueue = this.anaheimIdQueueFactory.Create(queueConfig);
            this.logger.Information(nameof(AnaheimIdQueueWorkerFactory), $"Create AnaheimId Queue Worker. accountName={queueConfig.AccountName}, queueName={queueConfig.AnaheimIdQueueName}");

            return new AnaheimIdQueueWorker(cloudQueue, this.pcfAdapter, this.configuration.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig, this.appConfiguration, this.logger);
        }
    }
}
