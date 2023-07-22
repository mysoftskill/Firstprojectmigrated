namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.ComplianceServices.Common;


#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// A collection of test hooks, used for helping our test automation out. These hooks are NOT compiled 
    /// for builds that go to production.
    /// </summary>
    [RoutePrefix("testhooks")]
    [ExcludeFromCodeCoverage]
    public class TestHooksController : ApiController
    {
        private ICosmosClient adlsCosmosClient;

        private ICosmosResourceFactory factory;

        /// <summary>
        /// Initializes a new instance of the class <see cref="TestHooksController" />.
        /// </summary>
        public TestHooksController()
        {
            ProductionSafetyHelper.EnsureNotInProduction();
            this.factory = new CosmosResourceFactory();
            this.adlsCosmosClient = this.factory.CreateCosmosAdlsClient(new AdlsConfig(
                    Config.Instance.Adls.AccountName,
                    Config.Instance.Adls.ClientAppId,
                    Config.Instance.Adls.AccountSuffix,
                    Config.Instance.Adls.TenantId,
                    CosmosClientFactory.GetMostRecentCertFromKeyVaultWithName(Config.Instance.Adls.ClientAppCertificateName).GetAwaiter().GetResult()) 
                );
        }


        /// <summary>
        /// Gets if the cosmos stream can be read.
        /// </summary>
        [HttpGet]
        [Route("pdms/canreadcosmosstream")]
        [IncomingRequestActionFilter("TestHooks", "CanReadCosmosStream", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanReadCosmosStream()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            string assetGroupStreamReaderFormat;
            bool isHourlyStream;
            if (await CommandFeedGlobals.AppConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCF.ReadHourlyPCFConfigStream).ConfigureAwait(false))
            {
                assetGroupStreamReaderFormat = CosmosAssetGroupInfoReader.AssetGroupInfoHourlyStreamFormat;
                isHourlyStream = true;
            }
            else
            {
                assetGroupStreamReaderFormat = CosmosAssetGroupInfoReader.AssetGroupInfoStreamFormat;
                isHourlyStream = false;
            }

            using (var variantStreamReader = await CosmosClientFactory.CreateLatestCosmosStructuredStreamReaderAsync(
                                                CosmosAssetGroupVariantInfoReader.AssetGroupVariantInfoStreamFormat
                                                    .Substring(CosmosAssetGroupVariantInfoReader.AssetGroupVariantInfoStreamFormat
                                                        .IndexOf("/local", StringComparison.InvariantCulture)), 
                                                    this.adlsCosmosClient, 90, this.factory))
            using (var assetGroupStreamReader = await CosmosClientFactory.CreateLatestCosmosStructuredStreamReaderAsync(
                assetGroupStreamReaderFormat.Substring(assetGroupStreamReaderFormat.IndexOf("/local",StringComparison.InvariantCulture)),
                    this.adlsCosmosClient, 90, this.factory, isHourlyStream))
            {
                var variantReader = new CosmosAssetGroupVariantInfoReader(variantStreamReader);
                var reader = new CosmosAssetGroupInfoReader(assetGroupStreamReader, variantReader);
                var result = reader.ReadAsync();
                var items = result.Result.AssetGroupInfos;

                DualLogger.Instance.Information(nameof(TestHooksController), $"Read ({items.Count}) pdms records.");

                if (items.Any(x => x?.AgentId == null))
                {
                    return this.BadRequest("Found null agent ID!");
                }

                DualLogger.Instance.Information(nameof(TestHooksController), "Verify asset groups parsing");
                foreach (var assetGroup in items)
                {
                    IAssetGroupInfo assetGroupInfo = new AssetGroupInfo(assetGroup, enableTolerantParsing: false);

                    DualLogger.Instance.Information(nameof(TestHooksController), $"AG: ({assetGroup.AgentId}/{assetGroup.AssetGroupId})");

                    DualLogger.Instance.Information(nameof(TestHooksController), $"SupportedDataTypes: {string.Join(",", assetGroupInfo.SupportedDataTypes)}");
                    DualLogger.Instance.Information(nameof(TestHooksController), $"SupportedCommandTypes: {string.Join(",", assetGroupInfo.SupportedCommandTypes)}");
                    DualLogger.Instance.Information(nameof(TestHooksController), $"SupportedSubjectTypes: {string.Join(",", assetGroupInfo.SupportedSubjectTypes)}");
                }
            }

            return this.Ok();
        }

        /// <summary>
        /// Gets if the variant info can be read from cosmos.
        /// </summary>
        [HttpGet]
        [Route("pdms/canreadvariantinfofromcosmos")]
        [IncomingRequestActionFilter("TestHooks", "CanReadVariantInfoFromCosmos", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanReadVariantInfoFromCosmos()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            using (var variantStreamReader = await CosmosClientFactory.CreateLatestCosmosStructuredStreamReaderAsync(
                CosmosAssetGroupVariantInfoReader.AssetGroupVariantInfoStreamFormat
                    .Substring(CosmosAssetGroupVariantInfoReader.AssetGroupVariantInfoStreamFormat.IndexOf("/local", StringComparison.InvariantCulture)),
                this.adlsCosmosClient, 90, this.factory))
            {
                var variantReader = new CosmosAssetGroupVariantInfoReader(variantStreamReader);
                var result = variantReader.Read();

                DualLogger.Instance.Information(nameof(TestHooksController), $"Read ({result.Count}) VariantInfo records.");

                DualLogger.Instance.Information(nameof(TestHooksController), "Verify VariantInfo parsing");
                foreach (var assetQualifier in result.Keys)
                {
                    var variants = result[assetQualifier];
                    DualLogger.Instance.Information(nameof(TestHooksController), $"AssetQualifier: ({assetQualifier}) has {variants.Count} of variantInfos");

                    foreach (var variant in variants)
                    {
                        AssetGroupVariantInfo variantInfo = new AssetGroupVariantInfo(variant, enableTolerantParsing: false);

                        DualLogger.Instance.Information(nameof(TestHooksController), $"VariantId: ({variant.VariantId})");
                        DualLogger.Instance.Information(nameof(TestHooksController), $"VariantName: ({variant.VariantName})");
                        DualLogger.Instance.Information(nameof(TestHooksController), $"VariantDescription: ({variant.VariantDescription})");
                        DualLogger.Instance.Information(nameof(TestHooksController), $"AssetGroupId: ({variant.AssetGroupId})");

                        DualLogger.Instance.Information(nameof(TestHooksController), $"ApplicableCapabilities: {string.Join(",", variantInfo.ApplicableCapabilities)}");
                        DualLogger.Instance.Information(nameof(TestHooksController), $"ApplicableDataTypeIds: {string.Join(",", variantInfo.ApplicableDataTypeIds)}");
                        DualLogger.Instance.Information(nameof(TestHooksController), $"ApplicableSubjectTypeIds: {string.Join(",", variantInfo.ApplicableSubjectTypes)}");
                    }
                }
            }

            return this.Ok();
        }


        /// <summary>
        /// Refreshes the data from PDMS.
        /// </summary>
        [HttpGet]
        [Route("backgroundtasks/refreshpdmsdata")]
        [IncomingRequestActionFilter("TestHooks", "PdmsDataRefresh", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> RefreshPdmsDataSet()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            PdmsDataRefreshTask task = new PdmsDataRefreshTask(this.adlsCosmosClient, 
                                                               this.factory, 
                                                               CommandFeedGlobals.AppConfiguration, 
                                                               new AlwaysUnlockedPrimitives<PdmsDataRefreshTask.LockState>());
            task.ForceRefreshData = true;

            await task.RunOnceAsync(null);
            return this.Ok();
        }

        /// <summary>
        /// Makes sure there are more than 1 database connections
        /// </summary>
        [HttpGet]
        [Route("pcf/canconfiguredatabaseconnection")]
        [IncomingRequestActionFilter("TestHooks", "CanConfigureDatabaseConnectionFromConfig", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult CanConfigureDatabaseConnectionFromConfig()
        {
            DualLogger.Instance.Information(nameof(TestHooksController), $"Getting database connection Info from config");
            var databaseConnectionList = DatabaseConnectionInfo.GetDatabaseConnectionInfosFromConfig();

            if (databaseConnectionList.Count <= 0)
            {
                return this.BadRequest("No database connection created");
            }

            DualLogger.Instance.Information(nameof(TestHooksController), $"Read ({databaseConnectionList.Count}) database connections from config.");
            foreach (var connection in databaseConnectionList)
            {
                DualLogger.Instance.Information(nameof(TestHooksController), $"Account Key: {connection.AccountKey} Account Uri: {connection.AccountUri} DatabaseId:DatabaseId: {connection.DatabaseId} DatabaseMoniker: {connection.DatabaseMoniker} Weight: {connection.Weight}");
            }
            return this.Ok();
        }

        /// <summary>
        /// Gets the AAD app ticket.
        /// </summary>
        [HttpGet]
        [Route("webhook/getaadappticket")]
        [IncomingRequestActionFilter("TestHooks", "AcquireAadAppTicket", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> AcquireAadAppTicket()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            AadCredentialProvider credentialProvider = new AadCredentialProvider();
            var ticket = await credentialProvider.GetAzureManagementTokenCredentialsAsync();

            if (ticket == null)
            {
                return this.InternalServerError();
            }

            return this.Ok();
        }

        /// <summary>
        /// Returns "OK" if the given agent ID and asset group ID combination is present.
        /// </summary>
        [HttpGet]
        [Route("pdms/agentmap/{agentIdString}/{assetGroupIdString}")]
        [IncomingRequestActionFilter("TestHooks", "QueryDataAgentMap", "1.0")]
        [DisallowPort80ActionFilter]
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        public async Task<IHttpActionResult> QueryDataAgentMapAsync(string agentIdString, string assetGroupIdString)
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            AgentId agentId = new AgentId(agentIdString);
            AssetGroupId assetGroupId = new AssetGroupId(assetGroupIdString);

            var map = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();

            if (!map.TryGetAgent(agentId, out IDataAgentInfo agentInfo))
            {
                return this.BadRequest($"Agent ID not found: {agentId}");
            }

            if (!agentInfo.TryGetAssetGroupInfo(assetGroupId, out _))
            {
                return this.BadRequest($"Asset group {assetGroupId} not found for agent {agentId}");
            }

            return this.Ok();
        }

        /// <summary>
        /// Cleans up the export storage.
        /// </summary>
        [HttpGet]
        [Route("backgroundtasks/exportstoragecleanup")]
        [IncomingRequestActionFilter("TestHooks", "ExportStorageCleanup", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CleanupExportStorage()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            ExportStorageCleanupTask task = new ExportStorageCleanupTask(new AlwaysUnlockedPrimitives<ExportStorageCleanupTask.LockState>());

            await task.RunOnceAsync(null);
            return this.Ok();
        }

        /// <summary>
        /// For completes a command.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        [HttpGet]
        [Route("backgroundtasks/oldexportforcecomplete/{commandId}")]
        [IncomingRequestActionFilter("TestHooks", "DailyForceComplete", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> OldExportForceComplete(string commandId)
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            OldExportForceCompleteTask task = new OldExportForceCompleteTask(
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.DataAgentMapFactory,
                CommandFeedGlobals.CosmosDbClientFactory,
                CommandFeedGlobals.AppConfiguration,
                new AlwaysUnlockedPrimitives<OldExportForceCompleteTask.LockState>());

            task.ScopedCommandId = new CommandId(commandId);

            await task.RunOnceAsync(null);
            return this.Ok();
        }

        /// <summary>
        /// Get agent azure storage queue depth.
        /// </summary>
        [HttpGet]
        [Route("backgroundtasks/checkagentazurestoragequeuedepth")]
        [IncomingRequestActionFilter("TestHooks", "CheckAgentAzureStorageQueueDepth", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CheckAgentAzureStorageQueueDepth()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            AzureQueueCommandQueueDepthChecker task = new AzureQueueCommandQueueDepthChecker(
                CommandFeedGlobals.DataAgentMapFactory,
                CommandFeedGlobals.CommandQueueFactory,
                CommandFeedGlobals.KustoTelemetryRepository,
                CommandFeedGlobals.Clock,
                new AlwaysUnlockedPrimitives<AzureQueueCommandQueueDepthChecker.LockState>());

            await task.RunOnceAsync(null);
            return this.Ok();
        }

        /// <summary>
        /// Performs ingestion recovery.
        /// </summary>
        [HttpPost]
        [Route("backgroundtasks/ingestionRecovery")]
        [IncomingRequestActionFilter("TestHooks", "IngestionRecovery", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> IngestionRecovery()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            // Create and insert record in CommandHistory in the past
            string rawRequestBody = await this.Request.Content.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<IDictionary<string, string>>(rawRequestBody);

            Identifier.TryParse(requestBody["agentId"], out AgentId agentId);
            Identifier.TryParse(requestBody["assetGroupId"], out AssetGroupId assetGroupId);
            string assetGroupQualifier = requestBody["assetGroupQualifier"];
            string rawCommand = requestBody["pxsCommand"];
            Enum.TryParse(requestBody["queueStorageType"], out QueueStorageType queueStorageType);
            queueStorageType = queueStorageType == QueueStorageType.Undefined ? QueueStorageType.AzureCosmosDb : queueStorageType;

            JObject pxsJObject = JObject.Parse(rawCommand);
            PXSV1.PrivacyRequest pxsRequest = pxsJObject.ToObject<PXSV1.PrivacyRequest>();

            var parser = new PxsCommandParser(agentId, assetGroupId, assetGroupQualifier, queueStorageType);
            var (pcfCommand, _) = parser.Process(pxsJObject);

            DateTimeOffset createdTime = DateTimeOffset.UtcNow.AddDays(-1.5);
            var record = new CommandHistoryRecord(pcfCommand.CommandId);
            record.Core.CreatedTime = createdTime;
            record.Core.Context = pxsRequest.Context;
            record.Core.Requester = pxsRequest.Requester;
            record.Core.IsSynthetic = false;
            record.Core.RawPxsCommand = rawCommand;
            record.Core.IsGloballyComplete = false;
            record.Core.IngestionDataSetVersion = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap().Version;
            record.Core.IngestionAssemblyVersion = EnvironmentInfo.AssemblyVersion;
            record.Core.TotalCommandCount = 1;
            record.Core.CommandType = pcfCommand.CommandType;
            record.Core.Subject = pcfCommand.Subject;
            record.Core.CompletedTime = null;
            record.Core.CompletedCommandCount = 0;
            record.Core.IngestedCommandCount = 0;
            record.Core.WeightedMonikerList = CommandMonikerHash.GetCurrentWeightedMonikers(queueStorageType);
            record.Core.QueueStorageType = queueStorageType;
            record.AuditMap[(agentId, assetGroupId)] = new CommandIngestionAuditRecord()
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.None,
                DebugText = "OK",
                IngestionStatus = CommandIngestionStatus.SendingToAgent
            };

            var preferredMoniker = CommandMonikerHash.GetPreferredMoniker(pcfCommand.CommandId, pcfCommand.AssetGroupId, record.Core.WeightedMonikerList);
            record.StatusMap[(agentId, assetGroupId)] = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = preferredMoniker
            };

            record.ClearDirty();

            ICommandHistoryRepository history = CommandFeedGlobals.CommandHistory;
            var success = await history.TryInsertAsync(record);

            if (!success)
            {
                return this.BadRequest();
            }

            // Trigger ingestion recovery
            var ingestionRecoveryWorkItem = new IngestionRecoveryWorkItem
            {
                ContinuationToken = null,
                NewestRecordCreationTime = createdTime.AddHours(1),
                OldestRecordCreationTime = createdTime.AddHours(-1),
                exportOnly = false
            };

            await CommandFeedGlobals.IngestionRecoveryWorkItemPublisher.PublishAsync(ingestionRecoveryWorkItem);

            // Return ok.
            return this.Ok();
        }

        /// <summary>
        /// Flushes a given agent's queue, clearing all commands issued until the flushdate
        /// </summary>
        [HttpGet]
        [Route("flushqueue/{agentIdValue}/{flushDateValue}")]
        [IncomingRequestActionFilter("TestHooks", "FlushAgentQueue", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult FlushAgentQueue(string agentIdValue, string flushDateValue = null)
        {
            return new FlushAgentQueueActionResult(
                this.Request,
                agentIdValue,
                flushDateValue,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.TestHooks,
                CommandFeedGlobals.AgentQueueFlushWorkItemPublisher);
        }

        /// <summary>
        /// Ensures that flighting is initialized.
        /// </summary>
        [HttpGet]
        [Route("flighting/initializationcheck")]
        [IncomingRequestActionFilter("TestHooks", "FlightingInitialized", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult FlightingInitialized()
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.FlightingEnabledTestHook))
            {
                return this.Ok();
            }

            return this.InternalServerError();
        }

        /// <summary>
        /// A distributed lock primitives that always says "yes".
        /// </summary>
        private class AlwaysUnlockedPrimitives<T> : IDistributedLockPrimitives<T> where T : class
        {
            public Task CreateIfNotExistsAsync()
            {
                return Task.FromResult(true);
            }

            public Task<DistributedLockStatus<T>> GetStatusAsync()
            {
                return Task.FromResult(new DistributedLockStatus<T>
                {
                    ExpirationTime = DateTime.MinValue,
                    OwnerId = null,
                    State = null,
                });
            }

            public Task<bool> TryAcquireOrExtendLeaseAsync(T value, DateTimeOffset expirationTime, string ownerId, string etag)
            {
                return Task.FromResult(true);
            }
        }

    }

#endif
}
