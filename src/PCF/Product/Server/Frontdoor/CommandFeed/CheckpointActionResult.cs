namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Identity;
    using Newtonsoft.Json;

    using ExportCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.ExportCommand;
    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

    /// <summary>
    /// Implements the Checkpoint API, which is how acknowledgments and lease extensions are communicated to PCF.
    /// </summary>
    internal class CheckpointActionResult : BaseHttpActionResult
    {
        private const int AgentStateMaxLength = 1024;

        // Determines the timeSpan jitter range rate.
        // If this rate is 0.5 and the base value is 100, then 50 is the jitter range. So the output should range from [75, 125];
        private const double JitterRangeRate = 0.333;

        // Compute minimum lease receipt version by required features.
        private static readonly long MinimumLeaseReceiptVersion = new[]
        {
            LeaseReceipt.MinimumCommandTypeVersion,
            LeaseReceipt.MinimumExpirationTimeVersion,
            LeaseReceipt.MinimumQualifierVersion,
            LeaseReceipt.MinimumCommandCreatedTimeVersion
        }.Max();

        private static readonly TimeSpan OneDayTimeSpan = TimeSpan.FromDays(1);
        private static readonly TimeSpan TenMinutesTimeSpan = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MaxCommandLifeSpan = TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays);
        private static readonly TimeSpan MaxCommandLifeSpanForAccountClose = TimeSpan.FromDays(90);

        protected enum CheckpointFinishAction
        {
            // Delete the queue item inline.
            InlineDelete = 0,

            // Delete the queue item in a deferred way.
            DeferredDelete = 1,

            // Replace the existing item inline.
            InlineReplace = 2,
        }

        private readonly ICommandQueue queue;
        private readonly AgentId agentId;
        private readonly AgentType agentType;
        private readonly HttpRequestMessage request;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IAuthorizer authorizer;
        private readonly ICommandLifecycleEventPublisher lifecyclePublisher;
        private readonly IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> deleteFromQueuePublisher;

        private LeaseReceipt leaseReceipt;
        private AsyncLazy<PrivacyCommand> lazyPrivacyCommand;
        private CheckpointRequest checkpointRequest;
        private IAssetGroupInfo assetGroupInfo;
        private readonly Common.ILogger logger;
        private readonly ICommandHistoryRepository repository;
        private readonly IApiTrafficHandler apiTrafficHandler;

        private const string TipAgentMessage = "Agent is Test-In-Production, completion record is not authoritative";

        protected IDictionary<PrivacyCommandStatus, Func<Task<CheckpointFinishAction>>> RequestStatusFunctionMap
        {
            get; set;
        }

        public CheckpointActionResult(
            AgentId agentId,
            IAuthorizer authorizer,
            ICommandQueue queue,
            IDataAgentMap dataAgentMap,
            ICommandLifecycleEventPublisher lifecyclePublisher,
            IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> deleteFromQueuePublisher,
            HttpRequestMessage checkpointRequest,
            Common.ILogger logger,
            ICommandHistoryRepository repository,
            IApiTrafficHandler apiTrafficHandler)
        {
            this.queue = queue;
            this.agentId = agentId;
            this.request = checkpointRequest;
            this.dataAgentMap = dataAgentMap;
            this.authorizer = authorizer;
            this.lifecyclePublisher = lifecyclePublisher;
            this.deleteFromQueuePublisher = deleteFromQueuePublisher;
            this.logger = logger;
            this.repository = repository;
            this.apiTrafficHandler = apiTrafficHandler;

            this.checkpointRequest = null;
            this.assetGroupInfo = null;

            this.agentType = this.agentId.GuidValue == Config.Instance.Frontdoor.CosmosExporterAgentId ? AgentType.Cosmos : AgentType.NonCosmos;

            // Request functions
            this.RequestStatusFunctionMap =
                new Dictionary<PrivacyCommandStatus, Func<Task<CheckpointFinishAction>>>
                {
                    [PrivacyCommandStatus.Pending] = this.PendingCheckPointRequestAsync,
                    [PrivacyCommandStatus.SoftDelete] = this.SoftDeleteCheckPointRequestAsync,
                    [PrivacyCommandStatus.Complete] = this.CompleteCheckPointRequestAsync,
                    [PrivacyCommandStatus.Deidentify] = this.CompleteCheckPointRequestAsync,
                    [PrivacyCommandStatus.Failed] = this.FailedCheckPointRequestAsync,
                    [PrivacyCommandStatus.UnexpectedCommand] = this.UnexpectedCommandCheckPointRequestAsync,
                    [PrivacyCommandStatus.VerificationFailed] = this.VerificationFailedCheckPointRequestAsync,
                    [PrivacyCommandStatus.UnexpectedVerificationFailure] = this.UnexpectedVerificationFailureCheckPointRequestAsync
                };
        }

        internal enum CheckpointErrorCode
        {
            InvalidLeaseExtension = 1,
            LeaseReceiptAgentIdMismatch = 2,
            CommandNotFound = 3,
            CommandAlreadyCompleted = 4,
            InvalidVariantsSpecified = 5,
            InvalidCommandStatus = 6,
            DelinkNotAllowed = 7,
            LeaseReceiptConflict = 8,
            MalformedLeaseReceipt = 9,
            UnknownPrivacyCommandStatus = 10,
            AgentStateExceedsMaxSizeAllowed = 11,
            LeaseReceiptAssetGroupIdMismatch = 12,
            LeaseReceiptNotSupported = 13,
            Throttle = 14, // Throttle from downstream service
            CommandAlreadyExpired = 15,
            TooManyRequests = 16 // Throttle by ApiTrafficThrottling
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.request, this.agentId);

            HttpContent content = null;
            string requestBody = null;
            try
            {
                content = this.request.Content;
                if (content == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"The request content is null.") };
                }

                requestBody = await this.request.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException hre)
            {
                // Content couldn't be read for some reason, lets get some log on the content length and then throw BadRequestException
                IncomingEvent.Current?.SetProperty("ContentLength", content?.Headers.ContentLength.ToString());

                throw new BadRequestException("The request content couldn't be read.", hre);
            }

            // API throttling 
            if (!this.apiTrafficHandler.ShouldAllowTraffic(ConfigNames.PCF.ApiTrafficPercantage, "PostCheckpoint", this.agentId.ToString()))
            {
                IncomingEvent.Current?.SetProperty("CheckpointErrorCode", CheckpointErrorCode.TooManyRequests.ToString());
                // return a 429 response
                return this.apiTrafficHandler.GetTooManyRequestsResponse();
            }

            try
            {
                this.checkpointRequest = JsonConvert.DeserializeObject<CheckpointRequest>(requestBody);
            }
            catch (JsonReaderException ex)
            {
                throw new BadRequestException($"Request cannot be parsed: {ex.GetType().Name}/{ex.Message}");
            }

            IncomingEvent.Current?.SetProperty("CheckpointStatusCode", this.checkpointRequest.Status);
            IncomingEvent.Current?.SetProperty("CommandId", this.checkpointRequest.CommandId);
            IncomingEvent.Current?.SetProperty("LeaseExtensionSeconds", this.checkpointRequest.LeaseExtensionSeconds.ToString());
            IncomingEvent.Current?.SetProperty("AffectedRows", this.checkpointRequest.RowCount.ToString());
            IncomingEvent.Current?.SetProperty("CheckpointRequestLeaseReceipt", this.checkpointRequest.LeaseReceipt);

            if (this.checkpointRequest.LeaseExtensionSeconds < 0)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.InvalidLeaseExtension);
            }

            var agentState = this.checkpointRequest.AgentState;
            if (agentState != null && agentState.Length > AgentStateMaxLength)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.AgentStateExceedsMaxSizeAllowed);
            }

            IncomingEvent.Current?.SetProperty("CheckpointAgentState", agentState?.Substring(0, Math.Min(100, agentState.Length)));

            if (!LeaseReceipt.TryParse(this.checkpointRequest.LeaseReceipt, out this.leaseReceipt))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.MalformedLeaseReceipt);
            }

            if (this.checkpointRequest.LeaseExtensionSeconds >= TimeSpan.FromDays(7).TotalSeconds && this.leaseReceipt.CommandType == Client.PrivacyCommandType.AgeOut)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.InvalidLeaseExtension);
            }

            StressRequestForwarder.Instance.SendForwardedRequest(
                authContext,
                this.request,
                new StringContent(requestBody),
                agentId: this.leaseReceipt.AgentId,
                assetGroupId: this.leaseReceipt.AssetGroupId,
                commandId: this.leaseReceipt.CommandId);

            IncomingEvent.Current?.SetProperty("ParsedLeaseReceipt", JsonConvert.SerializeObject(this.leaseReceipt));

            if (this.leaseReceipt.AgentId != this.agentId)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.LeaseReceiptAgentIdMismatch);
            }

            if (!this.queue.SupportsLeaseReceipt(this.leaseReceipt))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.LeaseReceiptNotSupported);
            }

            this.lazyPrivacyCommand = new AsyncLazy<PrivacyCommand>(() => this.repository.QueryPrivacyCommandAsync(this.leaseReceipt));

            // Make sure our lease receipt has all the data we need. Old lease receipts may be floating around
            // that don't have the full set of required data. In this case, we're forced to read the command again
            // to acquire a good lease receipt, but we use the existing etag the caller gave us.
            if (this.leaseReceipt.Version < MinimumLeaseReceiptVersion)
            {
                var oldEtag = this.leaseReceipt.Token;

                var command = await this.GetNonNullPrivacyCommandAsync();
                this.leaseReceipt = command.LeaseReceipt;

                if (this.leaseReceipt.Token != oldEtag)
                {
                    // Etag has already changed; no sense continuing.
                    return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.LeaseReceiptConflict);
                }

                if (this.leaseReceipt.Version < MinimumLeaseReceiptVersion)
                {
                    throw new InvalidOperationException("Couldn't fetch updated lease receipt. Something is not right.");
                }
            }

            IncomingEvent.Current?.SetProperty("LeaseRecieptAgentId", this.leaseReceipt.AgentId.Value);
            IncomingEvent.Current?.SetProperty("LeaseReceiptAssetGroupId", this.leaseReceipt.AssetGroupId.Value);
            IncomingEvent.Current?.SetProperty("LeaseReceiptCommandId", this.leaseReceipt.CommandId.Value);
            IncomingEvent.Current?.SetProperty("LeaseReceiptCommandCreatedTime", this.leaseReceipt.CommandCreatedTime.ToString());
            IncomingEvent.Current?.SetProperty("DatabaseMoniker", this.leaseReceipt.DatabaseMoniker);
            IncomingEvent.Current?.SetProperty("AssetGroupQualifier", this.leaseReceipt.AssetGroupQualifier);
            IncomingEvent.Current?.SetProperty("CommandType", this.leaseReceipt.CommandType.ToString());
            IncomingEvent.Current?.SetProperty("SubjectType", this.leaseReceipt.SubjectType.ToString());

            // Validate if the command has expired and return corresponding error code to client.
            var commandlifeSpan = this.leaseReceipt.CommandType == Client.PrivacyCommandType.AccountClose ? MaxCommandLifeSpanForAccountClose : MaxCommandLifeSpan;
            if (this.leaseReceipt.CommandCreatedTime?.Add(commandlifeSpan) <= DateTimeOffset.UtcNow)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.CommandAlreadyExpired);
            }

            if (!this.dataAgentMap.TryGetAgent(this.agentId, out IDataAgentInfo dataAgentInfo))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.LeaseReceiptAgentIdMismatch);
            }

            this.assetGroupInfo = dataAgentInfo.AssetGroupInfos.SingleOrDefault(x => x.AssetGroupId == this.leaseReceipt.AssetGroupId);
            if (this.assetGroupInfo == null)
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.LeaseReceiptAssetGroupIdMismatch);
            }

            if (this.checkpointRequest.Variants != null && this.checkpointRequest.Variants.Length > 0)
            {
                var command = await this.GetNonNullPrivacyCommandAsync();
                if (!command.AreClaimedVariantsValid(this.checkpointRequest.Variants, this.assetGroupInfo))
                {
                    return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.InvalidVariantsSpecified);
                }
            }

            if (!Enum.TryParse(this.checkpointRequest.Status, out PrivacyCommandStatus requestStatus))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.InvalidCommandStatus);
            }

            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "CheckpointCommandStatus").Increment(requestStatus.ToString());

            CheckpointFinishAction finishAction;
            if (this.RequestStatusFunctionMap.Keys.Contains(requestStatus))
            {
                finishAction = await this.RequestStatusFunctionMap[requestStatus]();
            }
            else
            {
                // Unexpected privacy command status
                return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.UnknownPrivacyCommandStatus);
            }

            IncomingEvent.Current?.SetProperty("FinishAction", finishAction.ToString());

            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            LeaseReceipt responseLeaseReceipt = null;
            try
            {
                if (finishAction == CheckpointFinishAction.InlineDelete)
                {
                    await this.queue.DeleteAsync(this.leaseReceipt);
                }
                else if (finishAction == CheckpointFinishAction.DeferredDelete)
                {
                    // 6 hours
                    int maxVisiblityDelaySeconds = 6 * 60 * 60;

                    // Cap the delay time at Lease Time * 75% or 6 hours, whichever is smaller
                    int visibilityDelaySeconds = (int)((this.leaseReceipt.ApproximateExpirationTime - DateTimeOffset.UtcNow).TotalSeconds * 0.75);
                    visibilityDelaySeconds = Math.Min(visibilityDelaySeconds, maxVisiblityDelaySeconds);

                    // Generate a random delay time that's less than the maximum
                    visibilityDelaySeconds = (int)((double)visibilityDelaySeconds * RandomHelper.NextDouble());

                    // Require a minimum 5-second delay
                    visibilityDelaySeconds = Math.Max(visibilityDelaySeconds, 5);

                    IncomingEvent.Current?.SetProperty("DeferralVisibilityDelay", visibilityDelaySeconds.ToString());
                    httpResponse.Headers.Add("X-NonTransactional-Checkpoint-Delay", visibilityDelaySeconds.ToString());

                    await this.deleteFromQueuePublisher.PublishAsync(
                        new DeleteFromQueueWorkItem(this.agentId, this.leaseReceipt),
                        TimeSpan.FromSeconds(visibilityDelaySeconds));
                }
                else if (finishAction == CheckpointFinishAction.InlineReplace)
                {
                    PrivacyCommand pcfCommand = await this.GetNonNullPrivacyCommandAsync();
                    IncomingEvent.Current?.SetProperty("UpdatedAgentState", pcfCommand.AgentState?.Substring(0, Math.Min(100, pcfCommand.AgentState.Length)));
                    IncomingEvent.Current?.SetProperty("UpdatedNextVisibleTime", pcfCommand.NextVisibleTime.ToString());

                    responseLeaseReceipt = await this.queue.ReplaceAsync(
                        this.leaseReceipt,
                        pcfCommand,
                        ConvertToCommandReplaceOperations(this.checkpointRequest));
                }
                else
                {
                    throw new InvalidOperationException("Unexpected checkpoint finish action: " + finishAction);
                }
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    return CreateErrorResponse(HttpStatusCode.Conflict, CheckpointErrorCode.LeaseReceiptConflict);
                }

                if (ex.ErrorCode == CommandFeedInternalErrorCode.NotFound)
                {
                    return CreateErrorResponse(HttpStatusCode.BadRequest, CheckpointErrorCode.CommandAlreadyCompleted);
                }

                if (ex.ErrorCode == CommandFeedInternalErrorCode.Throttle)
                {
                    return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, CheckpointErrorCode.Throttle);
                }

                throw;
            }

            httpResponse.Content = new JsonContent(
                new CheckpointResponse
                {
                    LeaseReceipt = responseLeaseReceipt?.Serialize()
                });

            return httpResponse;
        }

        internal static CommandReplaceOperations ConvertToCommandReplaceOperations(CheckpointRequest checkpointRequest)
        {
            CommandReplaceOperations replaceOperations = CommandReplaceOperations.None;

            if (!string.IsNullOrWhiteSpace(checkpointRequest?.AgentState))
            {
                replaceOperations |= CommandReplaceOperations.CommandContent;
            }

            // should extend lease if lease extension is specified
            if (checkpointRequest?.LeaseExtensionSeconds > 0)
            {
                replaceOperations |= CommandReplaceOperations.LeaseExtension;
            }

            // Should never replace nothing. At least do lease extension
            return replaceOperations == CommandReplaceOperations.None ? CommandReplaceOperations.LeaseExtension : replaceOperations;
        }

        private void LogExportFileSizes()
        {
            try
            {
                List<ExportedFileSizeDetails> checkpointRequestExportedFileSizeDetails = this.checkpointRequest.ExportedFileSizeDetails;
                if (checkpointRequestExportedFileSizeDetails != null)
                {
                    foreach (var exportedFile in checkpointRequestExportedFileSizeDetails)
                    {
                        AgentId originalAgentId = this.leaseReceipt.AgentId;

                        // In case of Cosmos Exporter real agent id has to be retrieved from asset group id
                        if (this.agentType == AgentType.Cosmos)
                        {
                            originalAgentId = ExtractOriginalAgentId(this.leaseReceipt.AssetGroupQualifier);
                        }

                        this.logger.LogExportFileSizeEvent(
                            originalAgentId,
                            this.leaseReceipt.AssetGroupId,
                            this.leaseReceipt.CommandId,
                            exportedFile.FileName,
                            exportedFile.OriginalSize,
                            exportedFile.CompressedSize,
                            exportedFile.IsCompressed,
                            this.leaseReceipt.SubjectType,
                            this.agentType,
                            this.leaseReceipt.CloudInstance);
                    }
                }
                else
                {
                    DualLogger.Instance.Warning(nameof(CheckpointActionResult), $"ExportedFileSizeDetails is empty for AgentId:{this.leaseReceipt.AgentId} AssetGroupId:{this.leaseReceipt.AssetGroupId} CommandId:{this.leaseReceipt.CommandId}");
                }
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentOutOfRangeException || ex is FormatException)
            {
                DualLogger.Instance.Error(nameof(CheckpointActionResult), ex, $"Failed to log export file sizes. AgentId:{this.leaseReceipt.AgentId} AssetGroupId:{this.leaseReceipt.AssetGroupId} CommandId:{this.leaseReceipt.CommandId}");
            }
        }

        /// <summary>
        ///     Extracts the original agent id from the asset group qualifier
        /// </summary>
        /// <param name="assetGroupQualifier">asset group qualifier</param>
        /// <returns>resulting Agent Id</returns>
        private static AgentId ExtractOriginalAgentId(string assetGroupQualifier)
        {
            AssetQualifier qualifier = AssetQualifier.Parse(assetGroupQualifier);
            string originalAgentId = null;

            originalAgentId = (qualifier.CustomProperties?.TryGetValue("OriginalAgentId", out originalAgentId) ?? false) ?
                originalAgentId :
                null;

            return originalAgentId != null ? new AgentId(originalAgentId) : null;
        }

        #region CheckpointRequest methods definition

        /// <summary>
        /// Gets the non-null privacy command or throws an exception.
        /// </summary>
        private async Task<PrivacyCommand> GetNonNullPrivacyCommandAsync()
        {
            PrivacyCommand value = await this.lazyPrivacyCommand;

            if (value == null)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError)
                {
                    Response =
                    {
                        Content = new JsonContent(new CheckpointError(CheckpointErrorCode.CommandNotFound))
                    }
                };
            }

            return value;
        }

        private async Task PublishLifeCycleEventAsync(PrivacyCommandStatus status, bool completedByPcf)
        {
            ICollection<string> failures = this.checkpointRequest.NonTransientFailures;

            if (this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                IncomingEvent.Current?.SetProperty("AgentReadinessState", "TestInProd");
                IncomingEvent.Current?.SetProperty("OriginalCheckpointStatusCode", this.checkpointRequest.Status);

                if (failures != null)
                {
                    failures = new List<string>(failures)
                    {
                        TipAgentMessage
                    };
                }
                else
                {
                    failures = new[] { TipAgentMessage };
                }
            }

            string requestNonTransientFailures = failures == null ? string.Empty : string.Join(";", failures);

            if (status == PrivacyCommandStatus.SoftDelete)
            {
                await this.lifecyclePublisher.PublishCommandSoftDeletedAsync(
                    this.agentId,
                    this.leaseReceipt.AssetGroupId,
                    this.leaseReceipt.AssetGroupQualifier,
                    this.leaseReceipt.CommandId,
                    this.leaseReceipt.CommandType,
                    this.leaseReceipt.CommandCreatedTime,
                    requestNonTransientFailures);
            }
            else if (status == PrivacyCommandStatus.Complete || status == PrivacyCommandStatus.Deidentify)
            {
                // Complete or Deidentify
                await this.lifecyclePublisher.PublishCommandCompletedAsync(
                    this.leaseReceipt.AgentId,
                    this.leaseReceipt.AssetGroupId,
                    this.leaseReceipt.AssetGroupQualifier,
                    this.leaseReceipt.CommandId,
                    this.leaseReceipt.CommandType,
                    this.leaseReceipt.CommandCreatedTime,
                    this.checkpointRequest.Variants,
                    ignoredByVariant: false,
                    rowCount: this.checkpointRequest.RowCount,
                    delinked: status == PrivacyCommandStatus.Deidentify,
                    nonTransientExceptions: requestNonTransientFailures,
                    completedByPcf: completedByPcf);
            }
        }

        private async Task UpdateExistingCommandAsync(
            DateTimeOffset nextVisibleTime,
            string agentState)
        {
            var command = await this.GetNonNullPrivacyCommandAsync();

            command.AgentState = agentState;
            command.NextVisibleTime = nextVisibleTime;
        }

        private async Task<CheckpointFinishAction> CompleteCheckPointRequestAsync()
        {
            return await this.CompleteCheckPointRequestHelperAsync(false);
        }

        private async Task<CheckpointFinishAction> CompleteCheckPointRequestHelperAsync(bool completedByPcf)
        {
            // Log as Complete so the report include deidentify checkpoint as complete.
            IncomingEvent.Current?.SetProperty("CheckpointStatusCode", PrivacyCommandStatus.Complete.ToString());

            // Update existing command and publish audit log
            await this.PublishLifeCycleEventAsync(PrivacyCommandStatus.Complete, completedByPcf);

            TimeSpan remainingLeaseTime = this.leaseReceipt.ApproximateExpirationTime - DateTimeOffset.UtcNow;
            int remainingTimeSeconds = (int)remainingLeaseTime.TotalSeconds;

            IncomingEvent.Current?.SetProperty("RemainingLeaseTimeSeconds", remainingTimeSeconds.ToString());

            // If export command log the exported file size details
            if (this.leaseReceipt.CommandType == Client.PrivacyCommandType.Export)
            {
                Task fireAndForget = Task.Run(
                    () =>
                    {
                        this.LogExportFileSizes();
                    },
                    CancellationToken.None);
            }

            // If we have some time before the lease receipt expires, then stick it in a queue instead of deleting inline. 
            // This allows us to amortize some spikes.
            bool hasEnoughLeaseTime = remainingLeaseTime > TenMinutesTimeSpan;

            // Check if deferred delete is disabled by flighting system
            bool isDeferredDeleteDisabled = FlightingUtilities.IsEnabled(FlightingNames.CheckpointCompleteDeferredDeleteDisabled);
            IncomingEvent.Current?.SetProperty("isDeferredDeleteDisabled", isDeferredDeleteDisabled.ToString());

            if (hasEnoughLeaseTime && !isDeferredDeleteDisabled)
            {
                return CheckpointFinishAction.DeferredDelete;
            }

            return CheckpointFinishAction.InlineDelete;
        }

        private async Task<CheckpointFinishAction> FailedCheckPointRequestAsync()
        {
            // if this agent is in TIP then don't enqueue but delete from queue
            if (this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                return await this.CompleteCheckPointRequestHelperAsync(true);
            }

            await this.lifecyclePublisher.PublishCommandFailedAsync(
                this.agentId,
                this.leaseReceipt.AssetGroupId,
                this.leaseReceipt.AssetGroupQualifier,
                this.leaseReceipt.CommandId,
                this.leaseReceipt.CommandType);

            var command = await this.GetNonNullPrivacyCommandAsync();
            if (command is ExportCommand exportCommand)
            {
                var containerStatusError = await ExportStorageManager.Instance.GetContainerErrorCodeAsync(
                    exportCommand.AzureBlobContainerTargetUri,
                    $"{exportCommand.AzureBlobContainerPath}/commandId.txt",
                    $"{exportCommand.CommandId},This file is included to validate Microsoft has write access to the Azure Storage prior to exporting data. You can ignore this file.");

                if (containerStatusError != null)
                {
                    // hotfix: fire-and-forgot method to checkpoint a force-completed command in case its container cannot be retrived
                    // In the case that we didn't create the container or if it no longer exists, we want to force complete the command.
                    this.DeleteCompletedCommand(command.CommandId);

                    DualLogger.Instance.Error(nameof(CheckpointActionResult), $"Container seems to be no longer available after agent {this.agentId} reported failure on command {this.leaseReceipt.CommandId}");
                    IncomingEvent.Current?.SetProperty("ContainerStatus", containerStatusError);

                    this.checkpointRequest.NonTransientFailures = (this.checkpointRequest.NonTransientFailures ?? Enumerable.Empty<string>())
                        .Concat(new[] { $"Export container status: {containerStatusError}" })
                        .ToArray();

                    await this.PublishLifeCycleEventAsync(PrivacyCommandStatus.Complete, completedByPcf: true);
                    return CheckpointFinishAction.InlineDelete;
                }
            }

            TimeSpan delay = ApplyJitter(TimeSpan.FromSeconds(Config.Instance.Frontdoor.Checkpoint.FailedCommandReplayTimeSecs), JitterRangeRate);
            DateTimeOffset nextVisibleTime = DateTimeOffset.UtcNow.AddSeconds(delay.TotalSeconds);

            await this.UpdateExistingCommandAsync(
                nextVisibleTime,
                this.checkpointRequest.AgentState);

            return CheckpointFinishAction.InlineReplace;
        }

        private async Task DeleteCompletedCommand(CommandId commandId)
        {
            try
            {
                CommandHistoryFragmentTypes fragmentsToRead = CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status;
                var record = await this.repository.QueryAsync(commandId, fragmentsToRead);
                if (record == null || record.StatusMap == null)
                {
                    return;
                }

                foreach (var item in record.StatusMap)
                {
                    var statusRecord = item.Value;
                    if (statusRecord == null)
                    {
                        continue;
                    }

                    if ((statusRecord.CompletedTime != null && statusRecord.CompletedTime != default(DateTimeOffset)) || statusRecord.ForceCompleted)
                    {
                        await this.PublishLifeCycleEventAsync(PrivacyCommandStatus.Complete, completedByPcf: true);
                        DualLogger.Instance.Information(nameof(CheckpointActionResult), $"Deleting command:{commandId},asset group={this.leaseReceipt.AssetGroupId},agent={this.agentId}");
                        await this.queue.DeleteAsync(this.leaseReceipt);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Can't directly check for Microsoft.Azure.Document.NotFoundException because it's not exposed, so check the message
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Resource Not Found"))
                {
                    // This is a fire-and-forget method, so it's possible the command has already been deleted...  log a warning without all the gory details.
                    DualLogger.Instance.Warning(nameof(CheckpointActionResult), $"Failure ocurred while checking if the command:{commandId} is force completed. Error Message: Resource Not Found.");
                }
                else
                {
                    DualLogger.Instance.Error(nameof(CheckpointActionResult), $"Failure ocurred while checking if the command:{commandId} is force completed. Error Message: {ex.InnerException}");
                }
            }
        }

        private async Task<CheckpointFinishAction> VerificationFailedCheckPointRequestAsync()
        {
            // if this agent is in TIP then don't enqueue but delete from queue
            if (this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                return await this.CompleteCheckPointRequestHelperAsync(true);
            }

            await this.lifecyclePublisher.PublishCommandVerificationFailedAsync(
                this.agentId,
                this.leaseReceipt.AssetGroupId,
                this.leaseReceipt.AssetGroupQualifier,
                this.leaseReceipt.CommandId,
                this.leaseReceipt.CommandType);

            // this is the case when either the verifier is invalid or the command is invalid. 
            // Eventually we may want to log and fail in this case rather than retry. 
            // For now lets retry since we just hit an unknown bug
            TimeSpan delay = ApplyJitter(TimeSpan.FromSeconds(Config.Instance.Frontdoor.Checkpoint.VerificationFailedReplayTimeSecs), JitterRangeRate);
            DateTimeOffset nextVisibleTime = DateTimeOffset.UtcNow.AddSeconds(delay.TotalSeconds);

            await this.UpdateExistingCommandAsync(
                nextVisibleTime,
                this.checkpointRequest.AgentState);

            return CheckpointFinishAction.InlineReplace;
        }

        private async Task<CheckpointFinishAction> UnexpectedVerificationFailureCheckPointRequestAsync()
        {
            // if this agent is in TIP then don't enqueue but delete from queue
            if (this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                return await this.CompleteCheckPointRequestHelperAsync(true);
            }

            await this.lifecyclePublisher.PublishCommandUnexpectedVerificationFailureAsync(
                this.agentId,
                this.leaseReceipt.AssetGroupId,
                this.leaseReceipt.AssetGroupQualifier,
                this.leaseReceipt.CommandId,
                this.leaseReceipt.CommandType);

            // This is the case where there was a problem with key discovery
            // lets retry after 2-4 hours to see if its fixed
            TimeSpan delay = ApplyJitter(TimeSpan.FromSeconds(Config.Instance.Frontdoor.Checkpoint.UnexpectedVerificationFailureReplayTimeSecs), JitterRangeRate);
            DateTimeOffset nextVisibleTime = DateTimeOffset.UtcNow.AddSeconds(delay.TotalSeconds);

            await this.UpdateExistingCommandAsync(
                nextVisibleTime,
                this.checkpointRequest.AgentState);

            return CheckpointFinishAction.InlineReplace;
        }

        private async Task<CheckpointFinishAction> UnexpectedCommandCheckPointRequestAsync()
        {
            // if this agent is in TIP then don't enqueue but delete from queue
            if (this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                return await this.CompleteCheckPointRequestHelperAsync(true);
            }

            await this.lifecyclePublisher.PublishCommandUnexpectedAsync(
                this.agentId,
                this.leaseReceipt.AssetGroupId,
                this.leaseReceipt.AssetGroupQualifier,
                this.leaseReceipt.CommandId,
                this.leaseReceipt.CommandType);

            // re-evaluate the filtering logic for the command, 
            var command = await this.GetNonNullPrivacyCommandAsync();

            bool isApplicable = this.assetGroupInfo.IsCommandActionable(command, out var applicabilityResult);
            if (
                this.assetGroupInfo.IsFakePreProdAssetGroup
                || !isApplicable
                || this.assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd)
            {
                DualLogger.Instance.Warning(nameof(CheckpointActionResult), $"Unexpected command ({this.leaseReceipt.CommandId}): {applicabilityResult.ReasonDescription}. AgentId: ({this.assetGroupInfo.AgentId}) AssetGroupId: ({this.assetGroupInfo.AssetGroupId})");

                await this.PublishLifeCycleEventAsync(PrivacyCommandStatus.Complete, completedByPcf: true);
                return CheckpointFinishAction.InlineDelete;
            }

            // Enqueue command back to replay
            TimeSpan delay = ApplyJitter(TimeSpan.FromSeconds(Config.Instance.Frontdoor.Checkpoint.UnexpectedCommandReplayTimeSecs), JitterRangeRate);
            DateTimeOffset nextVisibleTime = DateTimeOffset.UtcNow.AddSeconds(delay.TotalSeconds);
            await this.UpdateExistingCommandAsync(
                nextVisibleTime,
                string.Empty);

            // Log to SLL
            IncomingEvent.Current?.SetProperty("IsCommandActionableJustification", applicabilityResult.ReasonDescription);
            IncomingEvent.Current?.SetProperty("CommandDataTypes", command.DataTypeIds != null ? string.Join(",", command.DataTypeIds) : string.Empty);

            return CheckpointFinishAction.InlineReplace;
        }

        private async Task<CheckpointFinishAction> SoftDeleteCheckPointRequestAsync()
        {
            await this.PublishLifeCycleEventAsync(PrivacyCommandStatus.SoftDelete, completedByPcf: false);

            var command = await this.GetNonNullPrivacyCommandAsync();
            DateTimeOffset nextVisibleTime = CalculateNextVisibleTime(command, this.checkpointRequest.LeaseExtensionSeconds);

            await this.UpdateExistingCommandAsync(nextVisibleTime, this.checkpointRequest.AgentState);

            return CheckpointFinishAction.InlineReplace;
        }

        private async Task<CheckpointFinishAction> PendingCheckPointRequestAsync()
        {
            await this.lifecyclePublisher.PublishCommandPendingAsync(
                this.agentId,
                this.leaseReceipt.AssetGroupId,
                this.leaseReceipt.AssetGroupQualifier,
                this.leaseReceipt.CommandId,
                this.leaseReceipt.CommandType);

            var command = await this.GetNonNullPrivacyCommandAsync();
            DateTimeOffset nextVisibleTime = CalculateNextVisibleTime(command, this.checkpointRequest.LeaseExtensionSeconds);

            await this.UpdateExistingCommandAsync(nextVisibleTime, this.checkpointRequest.AgentState);

            return CheckpointFinishAction.InlineReplace;
        }

        #endregion

        /// <summary>
        /// Calculates the command next visible time.
        /// </summary>
        /// <returns>The next visible time.</returns>
        internal static DateTimeOffset CalculateNextVisibleTime(PrivacyCommand command, int requestedLeaseExtensionSeconds)
        {
            TimeSpan leaseExtensionDelay = TimeSpan.FromSeconds(requestedLeaseExtensionSeconds);
            DateTimeOffset requestedNextVisibleTime = command.NextVisibleTime.Add(leaseExtensionDelay);

            // Only apply the logic below for extensions under 1 day
            if (leaseExtensionDelay <= OneDayTimeSpan)
            {
                return requestedNextVisibleTime;
            }

            // Calculate 'safeVisibleHonoredThreshold', which represent a day 7 days before the command SLA expiration.
            DateTimeOffset safeVisibilityDelaySlaThreshold;
            var creationTime = command.Timestamp;
            if (command.CommandType == Client.PrivacyCommandType.Export)
            {
                if (command.Subject is AadSubject)
                {
                    safeVisibilityDelaySlaThreshold = creationTime.AddDays(Config.Instance.Frontdoor.Checkpoint.SafeVisibilityDelayAadExportSlaThresholdDays);
                }
                else
                {
                    // Non-AAD (MSA and Alt-subject) exports have a shorter SLA expectation of 21 days instead of 30, so the threshold is set to 14 days
                    safeVisibilityDelaySlaThreshold = creationTime.AddDays(Config.Instance.Frontdoor.Checkpoint.SafeVisibilityDelayExportSlaThresholdDays);
                }
            }
            else
            {
                safeVisibilityDelaySlaThreshold = creationTime.AddDays(Config.Instance.Frontdoor.Checkpoint.SafeVisibilityDelayNonExportSlaThresholdDays);
            }

            // if requested time redelivery is past the safe visibility threshold, redeliver on the next day.
            if (safeVisibilityDelaySlaThreshold < requestedNextVisibleTime)
            {
                return command.NextVisibleTime.Add(OneDayTimeSpan);
            }

            return requestedNextVisibleTime;
        }

        private static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, CheckpointErrorCode checkpointError)
        {
            IncomingEvent.Current?.SetProperty("CheckpointErrorCode", checkpointError.ToString());
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "CheckpointError").Increment(checkpointError.ToString());

            return new HttpResponseMessage(statusCode)
            {
                Content = new JsonContent(new CheckpointError(checkpointError))
            };
        }

        /// <summary>
        /// Applies jitter to a timeSpan value.
        /// If this rate is 0.5 and the base value is 100, then 50 is the jitter range.
        /// So the output should range between [75 ... 125];
        /// totalSeconds = 100;
        /// jitter = 100 * 0.5 * ([0 ... 1] - 0.5) = 100 * 0.5 * [-0.5 ... 0.5] = [-25 ... 25];
        /// return = 100 + [-25 ... 25] = [75 ... 125];
        /// </summary>
        private static TimeSpan ApplyJitter(TimeSpan timeSpan, double jitterRate)
        {
            double totalSeconds = timeSpan.TotalSeconds;
            double jitter = totalSeconds * jitterRate * (RandomHelper.NextDouble() - 0.5);
            return TimeSpan.FromSeconds(totalSeconds + jitter);
        }

        internal class CheckpointError
        {
            public CheckpointError(CheckpointErrorCode errorCode)
            {
                this.Message = errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public CheckpointErrorCode ErrorCode { get; set; }
        }
    }
}
