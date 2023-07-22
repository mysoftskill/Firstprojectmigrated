namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using System;
    using System.Threading;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierScanner;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleScanner;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleWorker;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;

    /// <summary>
    ///     IRecurrentDeleteQueueProcessorFactory
    /// </summary>
    public class RecurrentDeleteWorkerFactory : IRecurrentDeleteWorkerFactory
    {
        private readonly IRecurrentDeleteQueueFactory recurrentDeleteQueueFactory;
        private readonly IDistributedLockBlobPrimitivesFactory distributedLockBlobPrimitivesFactory;
        private readonly IPrivacyConfigurationManager configuration;
        private readonly IAppConfiguration appConfiguration;
        private readonly IScheduleDbClient scheduleDbClient;
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;
        private readonly ILogger logger;
        private readonly IPcfProxyService pcfProxyService;
        private readonly IPxfDispatcher PxfDispatcher;
        private readonly string recurringDeleteUamiId;

        /// <summary>
        /// RecurrentDeleteWorkerFactory
        /// </summary>
        /// <param name="recurrentDeleteQueueFactory"></param>
        /// <param name="lockBlobPrimitivesFactory"></param>
        /// <param name="configuration"></param>
        /// <param name="appConfiguration"></param>
        /// <param name="scheduleDbClient"></param>
        /// <param name="msaIdentityServiceAdapter"></param>
        /// <param name="pcfProxyService"></param>
        /// <param name="PxfDispatcher"></param>
        /// <param name="logger"></param>
        public RecurrentDeleteWorkerFactory(
            IRecurrentDeleteQueueFactory recurrentDeleteQueueFactory,
            IDistributedLockBlobPrimitivesFactory lockBlobPrimitivesFactory,
            IPrivacyConfigurationManager configuration,
            IAppConfiguration appConfiguration,
            IScheduleDbClient scheduleDbClient,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            IPcfProxyService pcfProxyService,
            IPxfDispatcher PxfDispatcher,
            ILogger logger)
        {
            this.recurrentDeleteQueueFactory = recurrentDeleteQueueFactory;
            this.distributedLockBlobPrimitivesFactory = lockBlobPrimitivesFactory;
            this.configuration = configuration;
            this.appConfiguration = appConfiguration;
            this.scheduleDbClient = scheduleDbClient;
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter;
            this.pcfProxyService = pcfProxyService;
            this.PxfDispatcher = PxfDispatcher;
            this.logger = logger;
            this.recurringDeleteUamiId = this.configuration.RecurringDeleteWorkerConfiguration.RecurringDeleteUamiId;
        }

        /// <inheritdoc />
        public IWorker CreatePreVerifierScanner(IAzureStorageConfiguration queueConfig)
        {
            var recurringDeleteWorkerConfig = this.configuration?.RecurringDeleteWorkerConfiguration ?? throw new ArgumentNullException(nameof(this.configuration.RecurringDeleteWorkerConfiguration));
            var distributedLockConfig = recurringDeleteWorkerConfig.DistributedLockConfiguration;

            var blockBlobPrimitives = this.distributedLockBlobPrimitivesFactory.CreateBloblockPrimitives(
                distributedLockConfig,
                distributedLockConfig.VerifierScannerBlobName,
                this.recurringDeleteUamiId);
                   
            DistributedLock<BaseRecurringDeleteScheduleDbScanner.LockState> distributedLock = new DistributedLock<DistributedBackgroundWorker.LockState>(
                distributedLockConfig.VerifierScannerBlobName,
                blockBlobPrimitives);

            var lockState = new BaseRecurringDeleteScheduleDbScanner.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(distributedLockConfig.MinLeaseTimeSeconds),
                TaskRunFrequency = TimeSpan.FromMinutes(distributedLockConfig.TaskRunFrequencyMintues),
                MaxExtensionTtl = TimeSpan.FromSeconds(distributedLockConfig.MaxExtensionTtlSeconds),
                ExtensionThreshold = TimeSpan.FromSeconds(distributedLockConfig.ExtensionThresholdSeconds),
                NextStartTime = DateTimeOffset.MinValue,
            };

            var cloudQueue = this.recurrentDeleteQueueFactory.CreatePreVerifierQueue(queueConfig, this.recurringDeleteUamiId);
            var cancellationTokenSource = new CancellationTokenSource();
            return new PreVerifierScanner(
                distributedLock,
                blockBlobPrimitives,
                lockState,
                cancellationTokenSource.Token,
                cloudQueue,
                this.appConfiguration,
                this.configuration,
                this.scheduleDbClient,
                this.logger);
        }

        /// <inheritdoc />
        public IWorker CreatePreVerifierWorker(IAzureStorageConfiguration queueConfig)
        {
            var cloudQueue = this.recurrentDeleteQueueFactory.CreatePreVerifierQueue(queueConfig, this.recurringDeleteUamiId);
            var cloudQueueConfig = this.configuration?.RecurringDeleteWorkerConfiguration?.CloudQueueConfig ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration.CloudQueueConfig));
            var scheduleDbConfig = this.configuration?.RecurringDeleteWorkerConfiguration?.ScheduleDbConfig ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration.ScheduleDbConfig));
            return new PreVerifierWorker(cloudQueue, cloudQueueConfig, scheduleDbConfig, this.appConfiguration, this.scheduleDbClient, this.msaIdentityServiceAdapter, this.logger);
        }

        /// <inheritdoc />
        public IWorker CreateRecurrentDeleteScheduleScanner(IAzureStorageConfiguration queueConfig)
        {
            var recurringDeleteWorkerConfig = this.configuration?.RecurringDeleteWorkerConfiguration ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration));
            var distributedLockConfig = recurringDeleteWorkerConfig.DistributedLockConfiguration;

            var blockBlobPrimitives = this.distributedLockBlobPrimitivesFactory.CreateBloblockPrimitives(
                            distributedLockConfig,
                            distributedLockConfig.ScheduleScannerBlobName,
                            this.recurringDeleteUamiId);
            DistributedLock<RecurrentDeleteScheduleScanner.LockState> distributedLock = new DistributedLock<DistributedBackgroundWorker.LockState>(
                distributedLockConfig.ScheduleScannerBlobName,
                blockBlobPrimitives);

            var lockState = new RecurrentDeleteScheduleScanner.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(distributedLockConfig.MinLeaseTimeSeconds),
                TaskRunFrequency = TimeSpan.FromMinutes(distributedLockConfig.TaskRunFrequencyMintues),
                MaxExtensionTtl = TimeSpan.FromSeconds(distributedLockConfig.MaxExtensionTtlSeconds),
                ExtensionThreshold = TimeSpan.FromSeconds(distributedLockConfig.ExtensionThresholdSeconds),
                NextStartTime = DateTimeOffset.MinValue,
            };

            var cloudQueue = this.recurrentDeleteQueueFactory.CreateScheduleQueue(queueConfig, this.recurringDeleteUamiId);
            var cancellationTokenSource = new CancellationTokenSource();
            return new RecurrentDeleteScheduleScanner(
                distributedLock, 
                blockBlobPrimitives,
                lockState,
                cancellationTokenSource.Token,
                cloudQueue,
                this.appConfiguration,
                this.configuration,
                this.scheduleDbClient,
                this.logger);
        }

        /// <inheritdoc />
        public IWorker CreateRecurrentDeleteScheduleWorker(IAzureStorageConfiguration queueConfig)
        {
            var cloudQueue = this.recurrentDeleteQueueFactory.CreateScheduleQueue(queueConfig, this.recurringDeleteUamiId);
            var cloudQueueConfig = this.configuration?.RecurringDeleteWorkerConfiguration?.CloudQueueConfig ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration.CloudQueueConfig));
            var scheduleDbConfig = this.configuration?.RecurringDeleteWorkerConfiguration?.ScheduleDbConfig ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration.ScheduleDbConfig));
            return new RecurrentDeleteScheduleWorker(
                cloudQueue: cloudQueue,
                cloudQueueConfiguration: cloudQueueConfig,
                scheduleDbConfiguration: scheduleDbConfig,
                appConfiguration: this.appConfiguration,
                scheduleDbClient: this.scheduleDbClient,
                pcfProxyService: this.pcfProxyService,
                pxfDispatcher: this.PxfDispatcher,
                this.logger);
        }
    }
}
