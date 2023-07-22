namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.EventHubs;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Publishes command lifecycle events to random event grids.
    /// </summary>
    public class CommandLifecycleEventPublisher : ICommandLifecycleEventPublisher
    {
        private readonly List<EventHubClient> eventHubClients;
        private readonly Dictionary<EventHubClient, string> eventHubClientNameMap;

        private readonly TimeSpan minimumRetryDelay = TimeSpan.FromMilliseconds(1);
        private readonly TimeSpan maximumRetryDelay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The maximum size of a single message that can be pushed into Event Hub.
        /// </summary>
        public static int MaxPublishSizeBytes => FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandLifecycleEventPublisher_MaxPublishBytes);

        /// <summary>
        /// Creates a new publisher using the two connection strings.
        /// </summary>
        public CommandLifecycleEventPublisher()
        {
            this.eventHubClients = new List<EventHubClient>();
            this.eventHubClientNameMap = new Dictionary<EventHubClient, string>();

            Configuration_CommandLifecycle_EventHub config = Config.Instance.CommandLifecycle.EventHub;
            foreach (var instance in config.Instances)
            {
                var client = EventHubClient.CreateFromConnectionString($"{instance.ConnectionString};EntityPath={instance.Path}");
                client.RetryPolicy = new RetryExponential(minimumRetryDelay, maximumRetryDelay, 5);

                this.eventHubClients.Add(client);
                this.eventHubClientNameMap[client] = instance.Moniker;
            }
        }

        #region Event publishers
        /// <inheritdoc />
        public Task PublishCommandRawDataAsync(
            IReadOnlyList<JObject> pxsCommands)
        {
            UpdateIsExportTestInProduction(pxsCommands);

            // Split pxsCommands into multiple chunks if the size > PxsCommandsBatchSize, to avoid failure while publishing to the event hub
            var pxsCommandChunkedList = BuildChunksWithRange(pxsCommands,
                FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandLifecycleEventPublisher_PxsCommandsBatchSize, defaultValue: 300));
            var publishTasks = new List<Task>();

            foreach (var request in pxsCommandChunkedList)
            {
                var batch = new[]
                {
                    CreateCommandRawDataEvent(request)
                };

                publishTasks.Add(this.PublishAsync(batch));
            }

            return Task.WhenAll(publishTasks);
        }

        /// <summary>
        /// Check if any incoming export command is a test command. If so, set IsExportTestInProduction property to true.
        /// TIP agents can only receive export commands that are marked IsExportTestInProduction = true
        /// </summary>
        /// <param name="pxsCommands">PXS commands.</param>
        /// <returns>List of processed pxs commands.</returns>
        private void UpdateIsExportTestInProduction(IReadOnlyList<JObject> pxsCommands)
        {
            foreach (var rawCommand in pxsCommands)
            {
                var (pcfCommand, _) = PxsCommandParser.DummyParser.Process(rawCommand);

                if (pcfCommand.CommandType == PrivacyCommandType.Export)
                {
                    bool isTestExport = false;
                    if (pcfCommand.Subject is AadSubject exportAadSubject)
                    {
                        if (FlightingUtilities.IsTenantIdEnabled(FlightingNames.TestInProductionByTenantIdEnabled, new TenantId(exportAadSubject.TenantId)))
                        {
                            isTestExport = true;
                        }
                    }
                    else if (pcfCommand.CommandSource == Portals.PartnerTestPage)
                    {
                        isTestExport = true;
                    }

                    if (isTestExport)
                    {
                        rawCommand["IsExportTestInProduction"] = true;
                        DualLogger.Instance.Information(nameof(PublishCommandRawDataAsync), $"Updated command {pcfCommand.CommandId} to IsExportTestInProduction = true");
                    }
                }
            }
        }

        /// <summary>
        /// Creates chunks from fullList depending upon the batchSize.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalList">Original list.</param>
        /// <param name="batchSize">Batch size.</param>
        /// <returns>List of chunks based on batchSize.</returns>
        private List<List<T>> BuildChunksWithRange<T>(IReadOnlyList<T> originalList, int batchSize)
        {
            if (originalList == null)
            {
                return null;
            }

            List<List<T>> chunkedList = new List<List<T>>();
            List<T> list = originalList.ToList();
            int index = 0;

            while (index < list.Count)
            {
                int remaining = list.Count - index;
                if (remaining >= batchSize)
                {
                    chunkedList.Add(list.GetRange(index, batchSize));
                }
                else
                {
                    chunkedList.Add(list.GetRange(index, remaining));
                }
                index += batchSize;
            }

            if (chunkedList.Count > 1)
            {
                DualLogger.Instance.Information(nameof(PublishCommandRawDataAsync),
                    $"OriginalPxsCommandsCount: {originalList.Count}, BatchSize: {batchSize}, TotalChunks: {chunkedList.Count}");
            }

            return chunkedList;
        }

        /// <inheritdoc />
        public Task PublishCommandStartedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            Uri finalExportDestinationUri,
            Uri stagingExportDestinationUri,
            string stagingExportDestinationPath,
            string assetGroupStreamName,
            string variantStreamName)
        {
            var batch = new[]
            {
                CreateStartedEvent(
                    agentId,
                    assetGroupId,
                    assetGroupQualifier,
                    commandId,
                    commandType,
                    commandCreationTime,
                    finalExportDestinationUri,
                    stagingExportDestinationUri,
                    stagingExportDestinationPath,
                    assetGroupStreamName,
                    variantStreamName)
            };

            return this.PublishAsync(batch);
        }

        /// <inheritdoc />
        public Task PublishCommandCompletedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string[] claimedVariants,
            bool ignoredByVariant,
            int rowCount,
            bool delinked,
            string nonTransientExceptions,
            bool completedByPcf,
            ForceCompleteReasonCode? forceCompleteReasonCode = null)
        {
            var batch = new[]
            {
                CreateCompletedEvent(
                    agentId,
                    assetGroupId,
                    assetGroupQualifier,
                    commandId,
                    commandType,
                    commandCreationTime,
                    claimedVariants,
                    ignoredByVariant,
                    rowCount,
                    delinked,
                    nonTransientExceptions,
                    completedByPcf,
                    forceCompleteReasonCode)
            };

            return this.PublishAsync(batch);
        }

        /// <inheritdoc />
        public Task PublishCommandSoftDeletedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string nonTransientExceptions)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateSoftDeleteEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType,
                        commandCreationTime,
                        nonTransientExceptions)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandSentToAgentAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateSentToAgentEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandPendingAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreatePendingEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandFailedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateFailedEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandUnexpectedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateUnexpectedEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandVerificationFailedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateVerificationFailedEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishCommandUnexpectedVerificationFailureAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return this.PublishAsync(
                new[]
                {
                    CreateUnexpectedVerificationFailureEvent(
                        agentId,
                        assetGroupId,
                        assetGroupQualifier,
                        commandId,
                        commandType)
                });
        }

        /// <inheritdoc />
        public Task PublishBatchAsync(CommandLifecycleEventBatch batch)
        {
            return this.PublishAsync(batch.Events);
        }

        internal static CommandRawDataEvent CreateCommandRawDataEvent(
            IReadOnlyList<JObject> pxsCommands)
        {
            return new CommandRawDataEvent
            {
                // AssetGroupId can NOT be Guid.Empty to avoid filter out by publisher logic.
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AgentId = new AgentId(Guid.Empty),
                AssetGroupQualifier = string.Empty,
                CommandId = new CommandId(Guid.Empty),
                CommandType = PrivacyCommandType.None,
                PxsCommands = pxsCommands
            };
        }
        #endregion

        #region Event builders
        internal static CommandDroppedEvent CreateCommandDroppedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            string notApplicableReasonCode,
            string assetGroupStreamName,
            string variantStreamName)
        {
            return new CommandDroppedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType,
                NotApplicableReasonCode = notApplicableReasonCode,
                AssetGroupStreamName = assetGroupStreamName,
                VariantStreamName = variantStreamName,
                AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandDroppedAuditLogAction(commandType)
            };
        }

        internal static CommandStartedEvent CreateStartedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            Uri finalExportDestinationUri,
            Uri stagingExportDestinationUri,
            string stagingExportDestinationPath,
            string assetGroupStreamName,
            string variantStreamName)
        {
            return new CommandStartedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType,
                CommandCreationTime = commandCreationTime,
                FinalExportDestinationUri = finalExportDestinationUri,
                ExportStagingDestinationUri = stagingExportDestinationUri,
                ExportStagingPath = stagingExportDestinationPath,
                AssetGroupStreamName = assetGroupStreamName,
                VariantStreamName = variantStreamName,
                AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandStartedAuditLogAction(commandType)
            };
        }

        internal static CommandCompletedEvent CreateCompletedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string[] claimedVariants,
            bool ignoredByVariant,
            int rowCount,
            bool delinked,
            string nonTransientExceptions,
            bool completedByPcf,
            ForceCompleteReasonCode? forceCompleteReasonCode)
        {
            CommandCompletedEvent completedEvent = new CommandCompletedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType,
                CommandCreationTime = commandCreationTime,
                AffectedRows = rowCount,
                Delinked = delinked,
                ClaimedVariantIds = claimedVariants ?? new string[0],
                IgnoredByVariant = ignoredByVariant,
                NonTransientExceptions = nonTransientExceptions,
                ForceCompleteReasonCode = forceCompleteReasonCode,
                CompletedByPcf = completedByPcf
            };

            completedEvent.AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandCompletedAuditLogAction(completedEvent);
            return completedEvent;
        }

        internal static CommandSoftDeleteEvent CreateSoftDeleteEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string nonTransientExceptions)
        {
            return new CommandSoftDeleteEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType,
                CommandCreationTime = commandCreationTime,
                NonTransientExceptions = nonTransientExceptions,
                AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandSoftDeleteAuditLogAction(commandType)
            };
        }

        internal static CommandSentToAgentEvent CreateSentToAgentEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandSentToAgentEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }

        internal static CommandPendingEvent CreatePendingEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandPendingEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }

        internal static CommandFailedEvent CreateFailedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandFailedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }

        internal static CommandUnexpectedEvent CreateUnexpectedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandUnexpectedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }

        internal static CommandVerificationFailedEvent CreateVerificationFailedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandVerificationFailedEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }

        internal static CommandUnexpectedVerificationFailureEvent CreateUnexpectedVerificationFailureEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType)
        {
            return new CommandUnexpectedVerificationFailureEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                AssetGroupQualifier = assetGroupQualifier,
                CommandId = commandId,
                CommandType = commandType
            };
        }
        #endregion

        private Task PublishAsync(IEnumerable<CommandLifecycleEvent> events)
        {
            if (!events.Any())
            {
                return Task.FromResult(true);
            }

#if INCLUDE_TEST_HOOKS
            // Don't publish events for the fake asset group.
            IEnumerable<EventData> toPublish =
                CommandLifecycleEventParser.Serialize(events.Where(e => e.AssetGroupId != Config.Instance.PPEHack.FixedAssetGroupId))
                .Select(x => x.ToEventData());
#else
            IEnumerable<EventData> toPublish =
                CommandLifecycleEventParser.Serialize(events).Select(x => x.ToEventData());
#endif
            // temporary logging introduced to determine ingestion issue will remove by 5/1/2023
            TemporaryLogging(events, "Events are being prepared for publishing:");

            var publishTasks = new List<Task>();
            while (toPublish.Any())
            {
                // set a limit of 500 KB per publish.
                int sizeRemaining = MaxPublishSizeBytes;
                var datas = new List<EventData>();

                while (toPublish.Any() && toPublish.First().Body.Count <= sizeRemaining && sizeRemaining > 0)
                {
                    EventData first = toPublish.First();
                    sizeRemaining -= first.Body.Count;

                    datas.Add(first);
                    toPublish = toPublish.Skip(1);
                }

                Task task = this.PublishAsync(events.First().CommandId, datas);
                publishTasks.Add(task);
            }

            return Task.WhenAll(publishTasks);
        }

        // TODO: remove by 5/1/2023 since it is only required for identifying an issue with ingestion.
        private void TemporaryLogging(IEnumerable<CommandLifecycleEvent> events, string additionalInfo = null)
        {
            List<string> logMessages = new List<string>
            {
                additionalInfo
            };

            // we need to cast to the child class to grab the commandId
            foreach (CommandLifecycleEvent evt in events)
            {
                logMessages.Add($"EventName={evt.EventName},CommandId={evt.CommandId},CommandType={evt.CommandType},AgentId={evt.AgentId},AssetGroupId={evt.AssetGroupId}");
            }
            additionalInfo = additionalInfo ?? string.Empty;

            string finalMessage = string.Join(Environment.NewLine, logMessages);

            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleEventPublisher), finalMessage);

        }

        private async Task PublishAsync(CommandId commandId, IEnumerable<EventData> events)
        {
            int eventCount = 0;
            int totalSizeBytes = 0;

            foreach (var @event in events)
            {
                eventCount++;
                totalSizeBytes += @event.Body.Count;
            }

            Exception lastException = null;
            bool isFailingClientRetry = false;

            EventHubClient client;
            this.TryGetRandomEventHubClient(null, out client);

            if (client == null)
            {
                throw new CommandFeedException("The EventHubClient was null. This may be a coding or configuration error.");
            }

            const int maxClientRetries = 3;
            for (int i = 0; i < maxClientRetries; i++)
            {
                try
                {
                    await Logger.InstrumentAsync(
                        new OutgoingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            ev["EventHubName"] = this.eventHubClientNameMap[client];
                            ev["EventCount"] = eventCount.ToString();
                            ev["TotalSizeBytes"] = totalSizeBytes.ToString();
                            ev["TryCount"] = i.ToString();

                            if (isFailingClientRetry)
                            {
                                ev["IsFailingClientRetry"] = true.ToString();
                            }

                            await client.SendAsync(events, commandId.Value);
                        });

                    // Once we've succeeded (ie, didn't throw), just return to the caller.
                    DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleEventPublisher), $"Events for commandId={commandId} have been successfully published");
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // Try using a different client
                    EventHubClient failingClient = client;
                    isFailingClientRetry = !this.TryGetRandomEventHubClient(failingClient, out client);

                    if (client == null)
                    {
                        throw new CommandFeedException("The EventHubClient was null. This may be a coding or configuration error.");
                    }
                }
            }

            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleEventPublisher), $"Events for commandId={commandId} have NOT been successfully published");

            throw new CommandFeedException("Unable to publish to any event hub!", lastException);
        }

        /// <summary>
        /// Picks an EventHubClient randomly from the list of configured event hubs
        /// </summary>
        /// <param name="failingEventHubClient">EventHubClient to avoid picking</param>
        /// <param name="chosenEventHubClient">Picked event hub client.</param>
        /// <returns>True if the chosenEventHubClient is different than the failingEventHubClient.</returns>
        private bool TryGetRandomEventHubClient(EventHubClient failingEventHubClient, out EventHubClient chosenEventHubClient)
        {
            int totalClientCount = this.eventHubClients.Count;
            if (totalClientCount <= 0)
            {
                throw new CommandFeedException("Configuration issue. There are no Event Hubs configured.");
            }

            const int maxTries = 5;
            int i = 0;

            do
            {
                int chosenIndex = RandomHelper.Next(0, totalClientCount);
                chosenEventHubClient = this.eventHubClients[chosenIndex];
                string clientName = eventHubClientNameMap[chosenEventHubClient];

                if (chosenEventHubClient == failingEventHubClient)
                {
                    // Try again, trying to avoid picking this client.
                    i++;
                    continue;
                }
                else if (FlightingUtilities.IsStringValueEnabled(FlightingNames.EventHubPublisherDisabled, clientName))
                {
                    // Try again, this client is disabled.
                    // Don't increase client try counter.
                    continue;
                }
                else
                {
                    return true;
                }
            } while (i < maxTries); // do-while executes at least once

            // Had to pick a failing client.
            // Statistically, this should happen in very low volume.
            // Example: 8 clients and 1 is failing. Chance of picking it again is (1/8)^maxTries = 0.000030517578125 = 0.003% (low volume to test the failing client)
            return false;
        }
    }
}
