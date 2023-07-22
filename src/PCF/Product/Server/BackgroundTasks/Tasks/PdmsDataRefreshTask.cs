namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    /// A periodic task that pulls Cosmos data in from PDMS.
    /// </summary>
    public class PdmsDataRefreshTask : WorkerTaskBase<object, PdmsDataRefreshTask.LockState>
    {
        private readonly ICosmosClient cosmosClient;

        private readonly ICosmosResourceFactory resourceFactory;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Creates a new instance of <see cref="PdmsDataRefreshTask"/>
        /// </summary>
        /// <param name="cosmosClient"></param>
        /// <param name="factory"></param>
        /// <param name="appConfiguration"></param>
        public PdmsDataRefreshTask(ICosmosClient cosmosClient, ICosmosResourceFactory factory, IAppConfiguration appConfiguration)
            : this(cosmosClient, factory, appConfiguration, (IDistributedLockPrimitives<LockState>)null)
        {
        }

        public PdmsDataRefreshTask(ICosmosClient cosmosClient, ICosmosResourceFactory factory, IAppConfiguration appConfiguration, IDistributedLockPrimitives<LockState> queueLockPrimitives)
            : base(Config.Instance.Worker.Tasks.PdmsCosmosRefresh.CommonConfig, nameof(PdmsDataRefreshTask), queueLockPrimitives)
        {
            this.cosmosClient = cosmosClient;
            this.resourceFactory = factory;
            this.appConfiguration = appConfiguration;
        }

        /// <summary>
        /// Indicates that data should be forcibily refreshed. Largely intended as a test hook.
        /// </summary>
        public bool ForceRefreshData { get; set; }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            return new Func<Task>[] { this.DoRefreshAsync };
        }

        private async Task DoRefreshAsync()
        {
            // adjust path for adls if required.
            string variantInfoStreamFormat = CosmosAssetGroupVariantInfoReader.AssetGroupVariantInfoStreamFormat;

            string assetGroupStreamReaderFormat;
            bool isHourlyStream;

            // if FeatureFlag for Hourly AssetGroupInfo is enabled, use the hourly stream format
            if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCF.ReadHourlyPCFConfigStream).ConfigureAwait(false))
            {
                assetGroupStreamReaderFormat = CosmosAssetGroupInfoReader.AssetGroupInfoHourlyStreamFormat;
                isHourlyStream = true;
            }
            else
            {
                assetGroupStreamReaderFormat = CosmosAssetGroupInfoReader.AssetGroupInfoStreamFormat;
                isHourlyStream = false;
            }
            
            if (this.cosmosClient.ClientTechInUse() == ClientTech.Adls)
            {
                variantInfoStreamFormat = variantInfoStreamFormat.Substring(variantInfoStreamFormat.IndexOf("/local", StringComparison.InvariantCulture));
                assetGroupStreamReaderFormat = assetGroupStreamReaderFormat.Substring(assetGroupStreamReaderFormat.IndexOf("/local", StringComparison.InvariantCulture));
            }

            using (var variantStreamReader = await CosmosClientFactory.CreateLatestCosmosStructuredStreamReaderAsync(variantInfoStreamFormat, this.cosmosClient, Config.Instance.PdmsCache.Cosmos.MaxCosmosStreamAgeDays, this.resourceFactory))
            using (var assetGroupStreamReader = await CosmosClientFactory.CreateLatestCosmosStructuredStreamReaderAsync(
                assetGroupStreamReaderFormat, this.cosmosClient, Config.Instance.PdmsCache.Cosmos.MaxCosmosStreamAgeDays, this.resourceFactory, isHourlyStream))
            {
                var variantReader = new CosmosAssetGroupVariantInfoReader(variantStreamReader);
                var assetGroupInfoReader = new CosmosAssetGroupInfoReader(assetGroupStreamReader, variantReader);
                var docDbCollection = await DocDbAssetGroupInfoCollection.CreateAsync();
                await docDbCollection.LoadFromAsync(assetGroupInfoReader, this.ForceRefreshData);
            }
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= DateTime.UtcNow;
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            var nextStartTime = DateTimeOffset.UtcNow.AddHours(Config.Instance.Worker.Tasks.PdmsCosmosRefresh.FrequencyHours);

            leaseTime = nextStartTime - DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
            return new LockState { NextStartTime = nextStartTime };
        }

        // State stored along with the lock.
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}