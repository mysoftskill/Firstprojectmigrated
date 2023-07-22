namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;
    using ILogger = Microsoft.PrivacyServices.CommandFeed.Service.Common.ILogger;

    internal class BatchCheckpointCompleteActionResult : BaseHttpActionResult
    {
        // The maximum number of checkpoint requests in a single batch
        internal const int MaximumBatchSize = 100;

        // Compute minimum lease receipt version by required features.
        private static readonly long MinimumLeaseReceiptVersion = new[]
        {
            LeaseReceipt.MinimumCommandTypeVersion,
            LeaseReceipt.MinimumExpirationTimeVersion,
            LeaseReceipt.MinimumQualifierVersion,
            LeaseReceipt.MinimumCommandCreatedTimeVersion,
        }.Max();

        private static readonly TimeSpan MaxCommandLifeSpan = TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays);

        private readonly AgentId agentId;

        private readonly AgentType agentType;

        private readonly IAuthorizer authorizer;

        private readonly IAzureWorkItemQueuePublisher<BatchCheckpointCompleteWorkItem> checkpointCompleteQueuePublisher;

        private readonly IDataAgentMap dataAgentMap;

        private readonly ICommandLifecycleEventPublisher lifecyclePublisher;

        private readonly ICommandQueue queue;

        private readonly HttpRequestMessage request;

        private readonly ILogger logger;

        private readonly ICommandHistoryRepository repository;

        private readonly IApiTrafficHandler apiTrafficHandler;

        public BatchCheckpointCompleteActionResult(
            AgentId agentId,
            IAuthorizer authorizer,
            ICommandQueue queue,
            IDataAgentMap dataAgentMap,
            ICommandLifecycleEventPublisher lifecyclePublisher,
            IAzureWorkItemQueuePublisher<BatchCheckpointCompleteWorkItem> checkpointCompleteQueuePublisher,
            HttpRequestMessage batchCheckpointCompleteRequest,
            ILogger logger,
            ICommandHistoryRepository repository,
            IApiTrafficHandler apiTrafficHandler)
        {
            this.queue = queue;
            this.agentId = agentId;
            this.dataAgentMap = dataAgentMap;
            this.authorizer = authorizer;
            this.lifecyclePublisher = lifecyclePublisher;
            this.checkpointCompleteQueuePublisher = checkpointCompleteQueuePublisher;
            this.request = batchCheckpointCompleteRequest;
            this.logger = logger;
            this.repository = repository;
            this.apiTrafficHandler = apiTrafficHandler;

            this.agentType = this.agentId.GuidValue == Config.Instance.Frontdoor.CosmosExporterAgentId ? AgentType.Cosmos : AgentType.NonCosmos;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            IncomingEvent.Current?.SetProperty("CheckAuthorizedAsyncStart", DateTimeOffset.UtcNow.ToString());
            await this.authorizer.CheckAuthorizedAsync(this.request, this.agentId);

            HttpContent content = null;
            string requestBody = null;
            try
            {
                content = this.request.Content;
                if (content == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"The request content is null.") };
                }

                // Some instrumentation to investigate content read error
                IncomingEvent.Current?.SetProperty("CurrentRequestsCount", CurrentRequestsCountMiddleware.GetCurrentRequestCount().ToString());
                IncomingEvent.Current?.SetProperty("ContentLength", content?.Headers.ContentLength.ToString());
                IncomingEvent.Current?.SetProperty("ContentType", content?.Headers.ContentType.MediaType);
                IncomingEvent.Current?.SetProperty("ReadAsStringAsyncStart", DateTimeOffset.UtcNow.ToString());

                requestBody = await this.request.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException hre)
            {
                // Content couldn't be read for some reason, throw BadRequestException
                throw new BadRequestException("The request content couldn't be read.", hre);
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                // The client must be facing issues.
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"The request content is null.") };
            }

            // API throttling
            if (!this.apiTrafficHandler.ShouldAllowTraffic(ConfigNames.PCF.ApiTrafficPercantage, "PostBatchCompleteCheckpoint", this.agentId.ToString()))
            {
                IncomingEvent.Current?.SetProperty("CheckpointCompleteErrorCode", CheckpointCompleteErrorCode.TooManyRequests.ToString());
                // return a 429 response
                return this.apiTrafficHandler.GetTooManyRequestsResponse();
            }

            IList<CheckpointCompleteRequest> checkpoints;
            try
            {
                checkpoints = JsonConvert.DeserializeObject<IList<CheckpointCompleteRequest>>(requestBody).ToList();
            }
            catch (JsonReaderException ex)
            {
                throw new BadRequestException($"Request cannot be parsed: {ex.GetType().Name}/{ex.Message}");
            }

            if (!checkpoints.Any())
            {
                // The client should send meaningful work.
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"The request content is empty.") };
            }

            // Limit maximum number of checkpoints in batch
            int batchSize = checkpoints.Count;
            IncomingEvent.Current?.SetProperty("CommandCount", batchSize.ToString());
            if (batchSize > MaximumBatchSize)
            {
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
                {
                    Content = new StringContent($"Please process a batch with a maximum size of {MaximumBatchSize}.")
                };
            }

            IncomingEvent.Current?.SetProperty("AgentId", this.agentId.Value);
            IncomingEvent.Current?.SetProperty("CommandIds", string.Join(",", checkpoints.Select(lr => lr.CommandId)));

            var errors = new ConcurrentDictionary<string, CheckpointCompleteResponse>();

            // Validate lease
            var leaseValidationTasks = new List<(CheckpointCompleteRequest request, Task<LeaseReceipt> task)>(batchSize);
            foreach (CheckpointCompleteRequest req in checkpoints)
            {
                Task<LeaseReceipt> leaseValidationTask = this.GetValidLeaseReceiptAsync(req, errors);
                leaseValidationTasks.Add((req, leaseValidationTask));
            }

            await Task.WhenAll(leaseValidationTasks.Select(x => x.task)).ConfigureAwait(false);

            if (errors.Count != 0)
            {
                string errorsStr = JsonConvert.SerializeObject(errors.Values);
                IncomingEvent.Current?.SetProperty("BatchCompleteErrors", errorsStr);

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(errorsStr, Encoding.UTF8, "application/json")
                };
            }

            // Only write to Audit log if all checkpoints are in good shape.
            var lifecycleTasks = new List<Task>(batchSize);
            var leaseReceipts = new List<LeaseReceipt>(batchSize);
            foreach ((CheckpointCompleteRequest checkpointRequest, Task<LeaseReceipt> leaseValidationTask) in leaseValidationTasks)
            {
                LeaseReceipt receipt = leaseValidationTask.Result;
                leaseReceipts.Add(receipt);
                string requestNonTransientFailures = checkpointRequest.NonTransientFailures == null ? string.Empty : string.Join(";", checkpointRequest.NonTransientFailures);

                // Write to the audit log as soon as possible
                Task lifecyleTask = this.lifecyclePublisher.PublishCommandCompletedAsync(
                    receipt.AgentId,
                    receipt.AssetGroupId,
                    receipt.AssetGroupQualifier,
                    receipt.CommandId,
                    receipt.CommandType,
                    receipt.CommandCreatedTime,
                    checkpointRequest.VariantIds?.ToArray(),
                    ignoredByVariant: false,
                    rowCount: checkpointRequest.RowCount,
                    delinked: false,
                    nonTransientExceptions: requestNonTransientFailures,
                    completedByPcf: false);
                lifecycleTasks.Add(lifecyleTask);

                if (receipt.CommandType == Client.PrivacyCommandType.Export)
                {
                    Task fireAndForget = Task.Run(
                        () =>
                        {
                            this.LogExportFileSizes(
                                receipt,
                                checkpointRequest.ExportedFileSizeDetails);
                        },
                        cancellationToken);
                }
            }

            await Task.WhenAll(lifecycleTasks).ConfigureAwait(false);

            // Calculates a lease visibility delay based on the lease receipt
            TimeSpan randomVisibilityDelay = CalculateRandomVisibilityDelay(leaseReceipts, DateTimeOffset.UtcNow, out double unused);

            // Now enqueue the completion request to the batch complete queue
            await this.checkpointCompleteQueuePublisher.PublishAsync(
                new BatchCheckpointCompleteWorkItem
                {
                    AgentId = this.agentId,
                    LeaseReceipts = leaseReceipts
                }, randomVisibilityDelay);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private void LogExportFileSizes(
            LeaseReceipt leaseReceipt,
            List<ExportedFileSizeDetails> exportedFileSizeDetails)
        {
            try
            {
                AgentId originalAgentId = leaseReceipt.AgentId;

                // In case of Cosmos Exporter real agent id has to be retrieved from asset group id
                if (this.agentType == AgentType.Cosmos)
                {
                    originalAgentId = ExtractOriginalAgentId(leaseReceipt.AssetGroupQualifier);
                }

                if (exportedFileSizeDetails != null)
                {
                    foreach (var exportedFile in exportedFileSizeDetails)
                    {
                        this.logger.LogExportFileSizeEvent(
                            originalAgentId,
                            leaseReceipt.AssetGroupId,
                            leaseReceipt.CommandId,
                            exportedFile.FileName,
                            exportedFile.OriginalSize,
                            exportedFile.CompressedSize,
                            exportedFile.IsCompressed,
                            leaseReceipt.SubjectType,
                            this.agentType,
                            leaseReceipt.CloudInstance);
                    }
                }
                else
                {
                    DualLogger.Instance.Warning(nameof(BatchCheckpointCompleteActionResult), $"ExportedFileSizeDetails is empty for AgentId:{leaseReceipt.AgentId} AssetGroupId:{leaseReceipt.AssetGroupId} CommandId:{leaseReceipt.CommandId}");
                    this.logger.LogExportFileSizeEvent(
                           originalAgentId,
                           leaseReceipt.AssetGroupId,
                           leaseReceipt.CommandId,
                           "NoFileSizeDetails",
                           0,
                           0,
                           false,
                           leaseReceipt.SubjectType,
                           this.agentType,
                           leaseReceipt.CloudInstance);
                }
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentOutOfRangeException || ex is FormatException)
            {
                DualLogger.Instance.Error(nameof(BatchCheckpointCompleteActionResult), ex, $"Failed to log export file sizes. AgentId:{leaseReceipt.AgentId} AssetGroupId:{leaseReceipt.AssetGroupId} CommandId:{leaseReceipt.CommandId}");
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

        private async Task<LeaseReceipt> GetValidLeaseReceiptAsync(CheckpointCompleteRequest checkpoint, IDictionary<string, CheckpointCompleteResponse> errors)
        {
            if (!Identifier.TryParse(checkpoint.CommandId, out CommandId commandId))
            {
                AddErrorToResponse(errors, checkpoint.CommandId, CheckpointCompleteErrorCode.MalformedLeaseReceipt, "One or more checkpointIds were null, white space or not parsable. ");
                return null;
            }

            if (!LeaseReceipt.TryParse(checkpoint.LeaseReceipt, out LeaseReceipt leaseReceipt))
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.MalformedLeaseReceipt);
                return null;
            }

            IncomingEvent.Current?.SetProperty("ParsedLeaseReceipt", JsonConvert.SerializeObject(leaseReceipt));

            if (leaseReceipt.AgentId != this.agentId)
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.LeaseReceiptAgentIdMismatch);
                return null;
            }

            if (leaseReceipt.CommandId != commandId)
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.MalformedLeaseReceipt);
                return null;
            }

            if (!this.queue.SupportsLeaseReceipt(leaseReceipt))
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.LeaseReceiptNotSupported);
                return null;
            }

            var lazyPrivacyCommand = new AsyncLazy<PrivacyCommand>(() => this.repository.QueryPrivacyCommandAsync(leaseReceipt));

            if (leaseReceipt.Version < MinimumLeaseReceiptVersion)
            {
                PrivacyCommand command = await lazyPrivacyCommand;
                if (command == null)
                {
                    AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.CommandNotFound);
                    return null;
                }

                leaseReceipt = command.LeaseReceipt;

                if (leaseReceipt.Version < MinimumLeaseReceiptVersion)
                {
                    throw new InvalidOperationException("Couldn't fetch updated lease receipt. Something is not right.");
                }
            }

            // Validate if the command is near expiration and return coresponding error code to client.
            // temporary shutdown for account close processing ( Account close is 90 day SLA )
            if (leaseReceipt.CommandCreatedTime?.Add(MaxCommandLifeSpan).AddHours(-1) < DateTimeOffset.UtcNow
                && leaseReceipt.CommandType != Client.PrivacyCommandType.AccountClose)
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.CommandAlreadyExpired);
                return null;
            }

            if (!this.dataAgentMap.TryGetAgent(this.agentId, out IDataAgentInfo dataAgentInfo))
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.LeaseReceiptAgentIdMismatch);
                return null;
            }

            IAssetGroupInfo assetGroupInfo = dataAgentInfo.AssetGroupInfos.SingleOrDefault(x => x.AssetGroupId == leaseReceipt.AssetGroupId);
            if (assetGroupInfo == null)
            {
                AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.LeaseReceiptAssetGroupIdMismatch);
                return null;
            }

            if (checkpoint.VariantIds != null && checkpoint.VariantIds.Any())
            {
                PrivacyCommand command = await lazyPrivacyCommand;
                if (command == null)
                {
                    AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.CommandNotFound);
                    return null;
                }

                if (!command.AreClaimedVariantsValid(checkpoint.VariantIds.ToArray(), assetGroupInfo))
                {
                    AddErrorToResponse(errors, commandId, CheckpointCompleteErrorCode.InvalidVariantsSpecified);
                    return null;
                }
            }

            return leaseReceipt;
        }

        /// <summary>
        /// Calculates a visibility delay based on time remaining in lease receipts.
        /// </summary>
        /// <param name="leaseReceipts">The lease receipts.</param>
        /// <param name="utcNow">The current time.</param>
        /// <param name="maxVisibilityDelaySecs">The calculated maximum visibility delay.</param>
        /// <returns>The visibility delay.</returns>
        internal static TimeSpan CalculateRandomVisibilityDelay(IEnumerable<LeaseReceipt> leaseReceipts, DateTimeOffset utcNow, out double maxVisibilityDelaySecs)
        {
            DateTimeOffset minimumApproximateExpirationTime = leaseReceipts.Min(lr => lr.ApproximateExpirationTime);
            double minimumRemainingLeaseTimeSecs = (minimumApproximateExpirationTime - utcNow).TotalSeconds;

            maxVisibilityDelaySecs = minimumRemainingLeaseTimeSecs / 4; // Allows for enough processing time for retries.

            // Cap to 6 hours
            if (maxVisibilityDelaySecs > 21600)
            {
                maxVisibilityDelaySecs = 21600;
            }
            else if (maxVisibilityDelaySecs < 0)
            {
                maxVisibilityDelaySecs = 0;
            }

            double visibilityDelaySecs = RandomHelper.NextDouble() * maxVisibilityDelaySecs; // Gives a range between 0 and maxVisibilityDelaySecs.
            return TimeSpan.FromSeconds(visibilityDelaySecs);
        }

        internal enum CheckpointCompleteErrorCode
        {
            LeaseReceiptAgentIdMismatch = 2,

            CommandNotFound = 3,

            InvalidVariantsSpecified = 5,

            MalformedLeaseReceipt = 9,

            LeaseReceiptAssetGroupIdMismatch = 12,

            LeaseReceiptNotSupported = 13,

            CommandAlreadyExpired = 15,

            TooManyRequests = 16
        }

        private static void AddErrorToResponse(IDictionary<string, CheckpointCompleteResponse> errors, CommandId commandId, CheckpointCompleteErrorCode errorCode, string customMessage = "")
        {
            string key = commandId == null ? "null" : commandId.Value;
            errors[key] = new CheckpointCompleteResponse { CommandId = key, Error = customMessage + errorCode };
        }

        private static void AddErrorToResponse(IDictionary<string, CheckpointCompleteResponse> errors, string commandIdStr, CheckpointCompleteErrorCode errorCode, string customMessage = "")
        {
            string key = string.IsNullOrWhiteSpace(commandIdStr) ? "null" : commandIdStr;
            errors[key] = new CheckpointCompleteResponse { CommandId = key, Error = customMessage + errorCode };
        }
    }
}
