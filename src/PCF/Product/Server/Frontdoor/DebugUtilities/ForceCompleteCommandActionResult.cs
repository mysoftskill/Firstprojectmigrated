namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;

    [ExcludeFromCodeCoverage] // Justification: not product code
    internal class ForceCompleteCommandActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage request;
        private readonly AuthenticationScope authenticationScope;
        private readonly CommandId commandId;
        private readonly ICommandLifecycleEventPublisher publisher;
        private readonly ICommandHistoryRepository commandHistoryRepository;
        private readonly IAuthorizer authorizer;
        private readonly ICosmosDbClientFactory cosmosDbClientFactory;
        private readonly IAppConfiguration appConfiguration;
        private ICosmosDbClient<CompletedExportEventEntry> completedExportEventCosmosDbClient;

        public ForceCompleteCommandActionResult(
            HttpRequestMessage requestMessage,
            CommandId commandId,
            ICommandHistoryRepository commandHistoryRepository,
            ICosmosDbClientFactory cosmosDbClientFactory,
            IAppConfiguration appConfiguration,
            ICommandLifecycleEventPublisher publisher,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope)
        {
            this.request = requestMessage;
            this.authenticationScope = authenticationScope;
            this.commandId = commandId;
            this.publisher = publisher;
            this.commandHistoryRepository = commandHistoryRepository;
            this.authorizer = authorizer;
            this.cosmosDbClientFactory = cosmosDbClientFactory;
            this.appConfiguration = appConfiguration;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);

            var record = await this.commandHistoryRepository.QueryAsync(this.commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status);            

            if (record?.Core == null)
            {
                // Record not found.
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            try
            {
                CommandLifecycleEventBatch batch = new CommandLifecycleEventBatch(this.commandId);

                // Publish a bunch of lifecycle events so that the command appears completed.
                IDataAgentMap dataAgentMap = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();

                if (Config.Instance.Common.IsTestEnvironment)
                {
                    ProductionSafetyHelper.EnsureNotInProduction();
                }
                else if (record.Core.CommandType != PrivacyCommandType.Export)
                {
                    throw new ArgumentException($"Command '{record.CommandId}' of type ${record.Core.CommandType} should not be force completed.");
                }

                // If the completed commands flag is enabled then the cosmos db client will be used for writing command ids
                if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCFV2.WriteCompletedCommands).ConfigureAwait(false))
                {
                    try
                    {
                        // Setup the cosmos db client if it hasn't been created yet
                        if (completedExportEventCosmosDbClient == null)
                        {
                            completedExportEventCosmosDbClient =
                                    cosmosDbClientFactory.GetCosmosDbClient<CompletedExportEventEntry>(Config.Instance.PCFv2.CosmosDbCompletedCommandsContainerName,
                                    Config.Instance.PCFv2.CosmosDbDatabaseName, Config.Instance.PCFv2.CosmosDbEndpointName);
                        }

                        // Convert the command id to guid to restore dashes then a string
                        string commandIdAsString = Guid.Parse(record.CommandId.Value).ToString();

                        // Add the command to the completed commands database for PCFV2
                        await completedExportEventCosmosDbClient.UpsertEntryAsync(new CompletedExportEventEntry()
                        {
                            Id = commandIdAsString
                        },
                        commandIdAsString).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                foreach (var item in record.StatusMap)
                {
                    // This agent+assetGroup completed the command.
                    if (item.Value.CompletedTime != null)
                    {
                        continue;
                    }

                    // This agent+assetGroup has NOT completed the command.
                    string assetGroupQualifier = string.Empty;
                    if (dataAgentMap.TryGetAgent(item.Key.agentId, out IDataAgentInfo dataAgentInfo) &&
                        dataAgentInfo.TryGetAssetGroupInfo(item.Key.assetGroupId, out IAssetGroupInfo assetGroupInfo))
                    {
                        assetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
                    }

                    batch.AddCommandCompletedEvent(
                        item.Key.agentId,
                        item.Key.assetGroupId,
                        assetGroupQualifier,
                        this.commandId,
                        record.Core.CommandType,
                        record.Core.CreatedTime,
                        new string[0],
                        false,
                        0,
                        delinked: false,
                        nonTransientExceptions: "Command force completed: " + Logger.Instance?.CorrelationVector,
                        completedByPcf: true,
                        forceCompleteReasonCode: ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage);

                    Logger.Instance?.LogForceCompleteCommandEvent(
                        record.CommandId,
                        item.Key.agentId,
                        item.Key.assetGroupId,
                        ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage.ToString(),
                        record.Core.CommandType,
                        record.Core.Subject?.GetSubjectType());

                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ManualExportForceCompleteItem").Increment();
                }
                
                // Check if command is globally completed even if there are no new events
                if (!batch.Events.Any())
                {
                    // Send completed event if we dont find any v1 agent
                    if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCFV2.WriteForceCompleteToEventHubWithNullValues).ConfigureAwait(false))
                    {
                        batch.AddCommandCompletedEvent(
                            new AgentId(Guid.Empty),
                            new AssetGroupId(Guid.Empty),
                            String.Empty,
                            this.commandId,
                            record.Core.CommandType,
                            record.Core.CreatedTime,
                            new string[0],
                            false,
                            0,
                            delinked: false,
                            nonTransientExceptions: "Command force completed: " + Logger.Instance?.CorrelationVector,
                            completedByPcf: true,
                            forceCompleteReasonCode: ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage);

                        PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ManualExportForceCompleteItem").Increment();
                    }

                    await new AzureWorkItemQueue<CheckCompletionWorkItem>()
                        .PublishAsync(new CheckCompletionWorkItem { CommandId = this.commandId });
                }

                await this.publisher.PublishBatchAsync(batch);
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ManualExportForceComplete").Increment();
            }
            catch (Exception)
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ManualExportForceComplete:Failed").Increment();
                throw;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
