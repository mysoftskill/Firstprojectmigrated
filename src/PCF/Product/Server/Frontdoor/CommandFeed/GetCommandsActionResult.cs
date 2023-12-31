namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.SignalApplicability;
    /// <summary>
    /// Implements the GetCommands API.
    /// 
    /// The behavior of this class is to do the following:
    ///  - Attempt to pop from high priority queues first.
    ///  - Return immediately if we've hit the maximum number of commands. PCF promises to not return more than 100.
    ///  - If after 500 milliseconds we have not found <see cref="MaxCommands"/> commands, but have found some commands, then return what commands we have found.
    ///  - From this point on, return as soon as we find any commands.
    ///  - If after x seconds we have not found any commands, then return an empty list.
    ///  - This logic is more complicated if the agent supports low priority queue (ie msa age out).
    ///  - If the agent supports low priority queue, the max wait time for high priority commands is reduced.
    ///  - After either finding commands from the high priority queue, or reaching the limit on wait time, the low priority queue is attempted.
    ///  - If after <see cref="MaxLowPriorityTimeDefault"/>' seconds we have not found any commands, then return an empty list.
    ///  - Additionally, flights control reducing the max wait time on the high priority queue - only if the agent supports low priority queue.
    ///  - See configured thresholds for more details: <see cref="MaxHighPriorityTimeReducedDefault"/>
    /// </summary>
    internal class GetCommandsActionResult : BaseHttpActionResult
    {
        /// <summary>
        /// The minimum duration of a lease we return to our caller. This is to avoid a situation where we return a lease
        /// without a meaningful opportunity to extend it.
        /// </summary>
        private static readonly TimeSpan MinimumLeaseDuration = TimeSpan.FromMinutes(1);

        private static readonly TimeSpan MaxHighPriorityTimeDefault = TimeSpan.FromSeconds(15);

        internal static readonly TimeSpan MaxHighPriorityTimeReducedDefault = TimeSpan.FromMilliseconds(500);

        private static readonly TimeSpan MaxLowPriorityTimeDefault = TimeSpan.FromSeconds(20);
        private const int MaxCommands = 100;

        private readonly ICommandQueueFactory queueFactory;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IAuthorizer authorizer;
        private readonly HttpRequestMessage requestMessage;
        private readonly DateTimeOffset startTime;
        private readonly AgentId agentId;
        private readonly ICommandLifecycleEventPublisher publisher;
        private readonly IApiTrafficHandler apiTrafficHandler;

        public GetCommandsActionResult(
            AgentId agentId,
            HttpRequestMessage requestMessage,
            ICommandQueueFactory queueFactory,
            IDataAgentMap dataAgentMap,
            ICommandLifecycleEventPublisher publisher,
            IAuthorizer authorizer,
            IApiTrafficHandler apiTrafficHandler)
        {
            this.queueFactory = queueFactory;
            this.requestMessage = requestMessage;
            this.agentId = agentId;
            this.dataAgentMap = dataAgentMap;
            this.authorizer = authorizer;
            this.publisher = publisher;
            this.apiTrafficHandler = apiTrafficHandler;
            this.startTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Never wait more than this much time for high priority.
        /// </summary>
        protected virtual bool HasMoreTimeHighPriority(TimeSpan maxHighPriorityTime)
        {
            return DateTimeOffset.UtcNow - this.startTime < maxHighPriorityTime;
        }

        /// <summary>
        /// Never wait more than this much time for low priority.
        /// </summary>
        protected virtual bool HasMoreTimeLowPriority => DateTimeOffset.UtcNow - this.startTime < MaxLowPriorityTimeDefault;

        /// <summary>
        /// Indicates if we've met the min time. Once we've met this time, and we have any commands, then send the response immediately.
        /// </summary>
        protected virtual bool HasMetMinTime => DateTimeOffset.UtcNow - this.startTime >= TimeSpan.FromMilliseconds(500);

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.agentId);
            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.requestMessage, null, this.agentId);

            if (!this.CheckClientVersion(out GetCommandsErrorCode error))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new GetCommandsError(error))
                };
            }

            if (FlightingUtilities.IsAgentBlocked(this.agentId))
            {
                IncomingEvent.Current?.SetProperty("AgentBlocked", "true");
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new JsonContent(new GetCommandsError(GetCommandsErrorCode.Blocked))
                };
            }

            if (FlightingUtilities.IsEnabled(FlightingNames.GetCommandsDisabled))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new JsonContent(new GetCommandsResponse())
                };
            }

            // API throttling
            if (!this.apiTrafficHandler.ShouldAllowTraffic(ConfigNames.PCF.ApiTrafficPercantage, "GetCommands", this.agentId.ToString()))
            {
                IncomingEvent.Current?.SetProperty("GetCommandsErrorCode", GetCommandsErrorCode.TooManyRequests.ToString());
                // return a 429 response
                return this.apiTrafficHandler.GetTooManyRequestsResponse();
            }

            if (TryGetRequestedLeaseDuration(this.requestMessage, out TimeSpan? requestedLeaseDuration))
            {
                requestedLeaseDuration = ValidateAndFilterLeaseDuration(requestedLeaseDuration);
            }

            GetCommandsResponse response = new GetCommandsResponse();
            DataAgentCommandQueue queue = new DataAgentCommandQueue(this.agentId, this.queueFactory, this.dataAgentMap);

            IDataAgentInfo dataAgentInfo = this.dataAgentMap[this.agentId];
            if (!dataAgentInfo.IsOnline)
            {
                await dataAgentInfo.MarkAsOnlineAsync();
            }

            bool isMultiTenantCollaborationSupported = ClientVersionHelper.DoesClientSupportMultiTenantCollaboration(this.requestMessage) &&
                                                       dataAgentInfo.IsOptedIntoAadSubject2();
            var serializerOption = isMultiTenantCollaborationSupported ? CommandFeedSerializerOptions.MultiTenantCollaborationSupported : CommandFeedSerializerOptions.None;
            var serializer = new CommandFeedSerializer(serializerOption);

            IncomingEvent.Current?.SetProperty("IsMultiTenantCollaborationSupported", isMultiTenantCollaborationSupported.ToString());

            var countsByAssetGroup = new Dictionary<AssetGroupId, GetCommandsCounter>();

            TimeSpan maxWaitTimeHighPriorityQueue = MaxHighPriorityTimeDefault;

            bool agentSupportsLowPriorityQueue = dataAgentInfo?.AssetGroupInfos != null &&
                                                 dataAgentInfo.AssetGroupInfos.Any(c => c != null && c.SupportsLowPriorityQueue);

            maxWaitTimeHighPriorityQueue = CalculateMaxWaitTimeHighPriorityQueue(agentSupportsLowPriorityQueue, maxWaitTimeHighPriorityQueue);

            int errorCount = await this.PopCommandsByPriorityAsync(
                () => this.HasMoreTimeHighPriority(maxWaitTimeHighPriorityQueue),
                CommandQueuePriority.High,
                response,
                queue,
                requestedLeaseDuration,
                dataAgentInfo,
                countsByAssetGroup,
                serializer,
                cancellationToken);

            // Only get low pri if not enough found from high pri AND agent supports low pri
            // TODO: May need to adjust the comparison count if no agent is able to pull from low pri queue.
            bool shouldPopLowPriority = response.Count <= 30 &&
                                        agentSupportsLowPriorityQueue;

            if (shouldPopLowPriority)
            {
                errorCount += await this.PopCommandsByPriorityAsync(
                    () => this.HasMoreTimeLowPriority,
                    CommandQueuePriority.Low,
                    response,
                    queue,
                    requestedLeaseDuration,
                    dataAgentInfo,
                    countsByAssetGroup,
                    serializer,
                    cancellationToken);
            }

            // Log per-assetgroup send stats.
            foreach (GetCommandsCounter counter in countsByAssetGroup.Values)
            {
                counter.Log();
            }

            // If we got no commands, and the error count was over our threshold, then let's mark it as a QOS error.
            if (response.Count == 0 && FlightingUtilities.IsIntegerValueEnabled(FlightingNames.GetCommandsPopErrorThreshold, errorCount))
            {
                IncomingEvent.Current?.SetForceReportAsFailed(true);
            }

            IncomingEvent.Current?.SetProperty("SupportsLowPriQueue", agentSupportsLowPriorityQueue.ToString());
            IncomingEvent.Current?.SetProperty("ShouldPopLowPriQueue", shouldPopLowPriority.ToString());
            IncomingEvent.Current?.SetProperty("ErrorCount", errorCount.ToString());
            IncomingEvent.Current?.SetProperty("CommandCount", response.Count.ToString());
            IncomingEvent.Current?.SetProperty("ExportCommandCount", response.ExportCommands.Count.ToString());
            IncomingEvent.Current?.SetProperty("AccountCloseCommandCount", response.AccountCloseCommands.Count.ToString());
            IncomingEvent.Current?.SetProperty("DeleteCommandCount", response.DeleteCommands.Count.ToString());
            IncomingEvent.Current?.SetProperty("AgeOutCommandCount", response.AgeOutCommands.Count.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(response)
            };
        }

        private async Task<int> PopCommandsByPriorityAsync(
            Func<bool> hasMoreTime,
            CommandQueuePriority priority,
            GetCommandsResponse response,
            DataAgentCommandQueue queue,
            TimeSpan? requestedLeaseDuration,
            IDataAgentInfo dataAgentInfo,
            Dictionary<AssetGroupId, GetCommandsCounter> countsByAssetGroup,
            CommandFeedSerializer serializer,
            CancellationToken cancellationToken)
        {
            int errorCount = 0;

            while (hasMoreTime.Invoke() && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int toPop = MaxCommands - response.Count;

                    CommandQueuePopResult popResult = await queue.PopAsync(toPop, requestedLeaseDuration, priority);
                    errorCount += popResult.Errors.Count;

                    foreach (Common.PrivacyCommand command in popResult.Commands)
                    {
                        bool knownAssetGroup = dataAgentInfo.TryGetAssetGroupInfo(command.AssetGroupId, out IAssetGroupInfo assetGroupInfo);

                        // Get or create commands counter
                        if (!countsByAssetGroup.TryGetValue(command.AssetGroupId, out GetCommandsCounter counter))
                        {
                            counter = new GetCommandsCounter(this.agentId, command.AssetGroupId, assetGroupInfo?.AssetGroupQualifier);
                            countsByAssetGroup[command.AssetGroupId] = counter;
                        }

                        // Skip asset group IDs that are blocked.
                        if (FlightingUtilities.IsAssetGroupIdBlocked(command.AssetGroupId))
                        {
                            counter.DroppedDueToBlockedAssetGroupCount++;
                            continue;
                        }

                        // Skip unknown asset groups
                        if (!knownAssetGroup)
                        {
                            counter.DroppedDueToUnknownAssetGroupIdCount++;
                            continue;
                        }

                        // Skip if the command is not actionable except in the cases 
                        // where the applicability result is tag dependent. In such a case, 
                        // lets trust the command ingestion applicability and send to agent 
                        // this is to prevent accidentally completing commands on behalf of the the agent
                        // if there are upstream issues in generating PcfConfig
                        if (!assetGroupInfo.IsCommandActionable(command, out ApplicabilityResult applicabilityResult))
                        {
                            // Log counts
                            if (counter.CompletedDueToApplicabilityReason.ContainsKey(applicabilityResult.ReasonCode))
                            {
                                counter.CompletedDueToApplicabilityReason[applicabilityResult.ReasonCode]++;
                            }
                            else
                            {
                                counter.CompletedDueToApplicabilityReason[applicabilityResult.ReasonCode] = 1;
                            }

                            if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.FilteredByVariant)
                            {
                                // there are any applicable variants and suppress command
                                List<IAssetGroupVariantInfo> applicableVariants =
                                    applicabilityResult.GetPcfApplicableVariants(assetGroupInfo.VariantInfosAppliedByPcf).ToList();
                                Task completeAndForgetWithVariants = this.BackgroundCompleteAndPublishCommandAsync(queue, command, applicableVariants);
                                continue;
                            }
                            else if (!ApplicabilityHelper.IsApplicabilityResultTagDependent(applicabilityResult.ReasonCode))
                            {
                                Task completeAndForget = this.BackgroundCompleteAndPublishCommandAsync(queue, command, null);
                                continue;
                            }
                        }

                        // update command with applicable properties
                        command.ApplicableVariants = command.GetCommandApplicableVariants(assetGroupInfo.VariantInfosAppliedByAgents).ToList();
                        command.DataTypeIds = applicabilityResult.ApplicableDataTypes;

                        // Skip commands with short lease duration
                        if (command.NextVisibleTime < DateTimeOffset.UtcNow + MinimumLeaseDuration)
                        {
                            counter.DroppedDueToShortLeaseCount++;
                            continue;
                        }

                        // Finally add command to response
                        response.Add(serializer.Process(command));
                        counter.CommandSentToAgent(command);
                        Task publishEventAndForget = this.publisher.PublishCommandSentToAgentAsync(
                            this.agentId,
                            command.AssetGroupId,
                            command.AssetGroupQualifier,
                            command.CommandId,
                            command.CommandType);
                    }

                    if (this.HasMetMinTime && response.Count > 0)
                    {
                        break;
                    }

                    if (response.Count >= MaxCommands)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Just ignore; we'll happily round-robin to the next queue.
                    Logger.Instance.UnexpectedException(ex);
                }
            }

            return errorCount;
        }

        /// <summary>
        /// Starts a task in the background to complete the given command.
        /// </summary>
        private async Task BackgroundCompleteAndPublishCommandAsync(
            ICommandQueue queue,
            Common.PrivacyCommand command,
            IEnumerable<IAssetGroupVariantInfo> applicableVariants)
        {
            bool ignoredByVariant = applicableVariants?.Any() == true;

            await this.publisher.PublishCommandCompletedAsync(
                command.AgentId,
                command.AssetGroupId,
                command.AssetGroupQualifier,
                command.CommandId,
                command.CommandType,
                command.Timestamp,
                applicableVariants?.Select(vn => vn.VariantId.Value).ToArray(),
                ignoredByVariant: ignoredByVariant,
                rowCount: 0,
                delinked: false,
                nonTransientExceptions: null,
                completedByPcf: true);

            // delete the completed command from the queue
            await queue.DeleteAsync(command.LeaseReceipt);
        }

        private bool CheckClientVersion(out GetCommandsErrorCode errorCode)
        {
            string clientVersionHeader = ClientVersionHelper.GetClientVersionHeader(this.requestMessage);

            if (!string.IsNullOrWhiteSpace(clientVersionHeader))
            {
                if (clientVersionHeader.StartsWith("pcfsdk", StringComparison.OrdinalIgnoreCase))
                {
                    string[] chunks = clientVersionHeader.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (chunks.Any(chunk => chunk.Equals("v:false")) && !Config.Instance.Frontdoor.AllowSdkWithoutVerifier)
                    {
                        errorCode = GetCommandsErrorCode.EnforceValidationRequired;
                        return false;
                    }
                }
            }

            errorCode = GetCommandsErrorCode.Ok;
            return true;
        }

        private static TimeSpan CalculateMaxWaitTimeHighPriorityQueue(bool agentSupportsLowPriorityQueue, TimeSpan maxWaitTimeHighPriorityQueue)
        {
            if (agentSupportsLowPriorityQueue)
            {
                // Reduce the max wait time on high pri queue.
                // This is intended to prevent waiting for the max wait time all the time
                maxWaitTimeHighPriorityQueue = MaxHighPriorityTimeReducedDefault;
            }

            return maxWaitTimeHighPriorityQueue;
        }

        /// <summary>
        /// Tries to extract the custom lease duration from the request headers.
        /// </summary>
        /// <param name="request">The http request.</param>
        /// <param name="requestedLeaseDuration">The nullable lease duration. Null value if no timeout value is found.</param>
        /// <returns>True if a valid lease duration is found.</returns>
        private static bool TryGetRequestedLeaseDuration(HttpRequestMessage request, out TimeSpan? requestedLeaseDuration)
        {
            requestedLeaseDuration = null;
            if (!request.Headers.TryGetValues("x-lease-duration-seconds", out IEnumerable<string> requestedLeaseDurationValues))
            {
                return false;
            }

            string requestedLeaseDurationStr = requestedLeaseDurationValues.FirstOrDefault();
            if (requestedLeaseDurationStr == null)
            {
                return false;
            }

            if (!int.TryParse(requestedLeaseDurationStr, out int leaseDurationSecs))
            {
                return false;
            }

            requestedLeaseDuration = TimeSpan.FromSeconds(leaseDurationSecs);
            return true;
        }

        /// <summary>
        /// Validates and filters the lease duration.
        /// </summary>
        /// <param name="requestedLeaseDuration">The lease duration.</param>
        /// <returns>The valid lease duration.</returns>
        private static TimeSpan? ValidateAndFilterLeaseDuration(TimeSpan? requestedLeaseDuration)
        {
            if (!requestedLeaseDuration.HasValue)
            {
                return null;
            }

            int minLeaseDurationSecs = Config.Instance.CosmosDBQueues.MinLeaseDurationSeconds;
            int maxLeaseDurationSecs = Config.Instance.CosmosDBQueues.MaxLeaseDurationSeconds;

            if (requestedLeaseDuration.Value.TotalSeconds > maxLeaseDurationSecs)
            {
                return null;
            }

            if (requestedLeaseDuration.Value.TotalSeconds < minLeaseDurationSecs)
            {
                return null;
            }

            return requestedLeaseDuration;
        }

        internal enum GetCommandsErrorCode
        {
            Ok = 0,
            ClientVersionTooOld = 1,
            EnforceValidationRequired = 2,
            Blocked = 3,
            TooManyRequests = 4
        }

        internal class GetCommandsError
        {
            public GetCommandsError(GetCommandsErrorCode errorCode)
            {
                this.Message = errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public GetCommandsErrorCode ErrorCode { get; set; }
        }

        /// <summary>
        /// A per-asset group command counter and logger.
        /// </summary>
        private class GetCommandsCounter
        {
            private readonly AgentId agentId;
            private readonly AssetGroupId assetGroupId;
            private readonly string assetGroupQualifier;
            private readonly List<CommandId> commandIds;
            private readonly Dictionary<PrivacyCommandType, int> commandCountsByType;

            public GetCommandsCounter(AgentId agentId, AssetGroupId assetGroupId, string assetGroupQualifier)
            {
                this.agentId = agentId;
                this.assetGroupId = assetGroupId;
                this.commandIds = new List<CommandId>();
                this.commandCountsByType = new Dictionary<PrivacyCommandType, int>();
                this.assetGroupQualifier = assetGroupQualifier;
                this.CompletedDueToApplicabilityReason = new Dictionary<ApplicabilityReasonCode, int>();

                if (string.IsNullOrEmpty(this.assetGroupQualifier))
                {
                    this.assetGroupQualifier = "(unknown)";
                }
            }

            public int DroppedDueToShortLeaseCount { get; set; }

            public int DroppedDueToBlockedAssetGroupCount { get; set; }

            public int DroppedDueToUnknownAssetGroupIdCount { get; set; }

            public IDictionary<ApplicabilityReasonCode, int> CompletedDueToApplicabilityReason { get; }

            public void CommandSentToAgent(Common.PrivacyCommand command)
            {
                if (!this.commandCountsByType.ContainsKey(command.CommandType))
                {
                    this.commandCountsByType[command.CommandType] = 0;
                }

                this.commandCountsByType[command.CommandType]++;
                this.commandIds.Add(command.CommandId);
            }

            public void Log()
            {
                Logger.Instance?.CommandsReturned(
                    this.agentId,
                    this.assetGroupId,
                    this.assetGroupQualifier,
                    this.commandIds,
                    this.commandCountsByType);

                this.Log("GetCommandsReturnedToAgent", this.commandIds.Count);
                this.Log("GetCommandsDroppedDueToShortLease", this.DroppedDueToShortLeaseCount);
                this.Log("GetCommandsDroppedDueToBlockedAssetGroupCount", this.DroppedDueToBlockedAssetGroupCount);
                this.Log("GetCommandsDroppedDueToUnknownAssetGroupIdCount", this.DroppedDueToUnknownAssetGroupIdCount);

                foreach (var applicabilityCounts in this.CompletedDueToApplicabilityReason)
                {
                    this.Log("GetCommandsDroppedDueTo" + applicabilityCounts.Key, applicabilityCounts.Value);
                }
            }

            private void Log(string transferPoint, int count)
            {
                if (count > 0)
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, transferPoint).Increment(count);

                    Logger.Instance?.CommandsTransferred(
                        count,
                        this.agentId.Value,
                        this.assetGroupId.Value,
                        transferPoint);
                }
            }
        }
    }
}
