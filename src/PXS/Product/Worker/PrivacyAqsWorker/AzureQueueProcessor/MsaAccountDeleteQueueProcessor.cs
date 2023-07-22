// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Ms.Qos;

    public class MsaAccountDeleteQueueProcessor : BackgroundWorker
    {
        private const int DequeueCountErrorThreshold = 5;

        private const int RequestCount = 100;

        private static readonly TimeSpan pcfFailureLeaseExtensionTime = TimeSpan.FromMinutes(30);

        private static readonly TimeSpan requestBuilderFailureLeaseExtensionTime = TimeSpan.FromMinutes(15);

        private readonly ICounterFactory counterFactory;

        private readonly bool ignoreVerifierErrors;

        private readonly ILogger logger;

        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        private readonly IAccountDeleteWriter pcfWriter;

        private readonly IMsaAccountDeleteQueue queue;

        private readonly string requesterId;

        private readonly IVerificationTokenValidationService tokenValidationService;

        private readonly IXboxAccountsAdapter xboxAccountsAdapter;

        public MsaAccountDeleteQueueProcessor(
            IMsaAccountDeleteQueueProcessorConfiguration config,
            IMsaAccountDeleteQueue queue,
            IXboxAccountsAdapter xboxAccountsAdapter,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            IVerificationTokenValidationService tokenValidationService,
            IAccountDeleteWriter pcfWriter,
            ICounterFactory counterFactory,
            ILogger logger)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.xboxAccountsAdapter = xboxAccountsAdapter ?? throw new ArgumentNullException(nameof(xboxAccountsAdapter));
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
            this.tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
            this.pcfWriter = pcfWriter ?? throw new ArgumentNullException(nameof(pcfWriter));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.requesterId = config?.RequesterId ?? throw new ArgumentNullException(nameof(config.RequesterId));
            this.ignoreVerifierErrors = config?.IgnoreVerifierErrors ?? throw new ArgumentNullException(nameof(config.IgnoreVerifierErrors));
        }

        /// <inheritdoc />
        public override Task<bool> DoWorkAsync()
        {
            var apiEvent = new IncomingApiEventWrapper();
            apiEvent.Start(nameof(MsaAccountDeleteQueueProcessor));
            apiEvent.CallerName = "Worker";

            return DoWorkInstrumentedAsync(
                apiEvent,
                async ev =>
                {
                    // 1. Get requests from queue.
                    IList<IQueueItem<AccountDeleteInformation>> queueMessages = null;
                    try
                    {
                        queueMessages = await this.queue.GetMessagesAsync(RequestCount, CancellationToken.None).ConfigureAwait(false);
                        ev.Success = true;
                        this.logger.Verbose(nameof(MsaAccountDeleteQueueProcessor), $"{queueMessages?.Count.ToString() ?? "NULL"} messages found in queue.");

                        if (queueMessages == null)
                        {
                            ev.ExtraData["GetMessagesCount"] = "NULL";

                            // No messages found.
                            return false;
                        }

                        ev.ExtraData["GetMessagesCount"] = queueMessages.Count.ToString();

                        if (queueMessages.Count == 0)
                        {
                            // No messages found.
                            return false;
                        }

                        ev.ExtraData["DequeueCounts"] = string.Join(",", queueMessages.Select(c => c.DequeueCount));
                        LogDequeueCountThresholdErrors(queueMessages, ev);
                    }
                    catch (Exception e)
                    {
                        string errorMessage = $"Failed to read from queue. Exception: {e}.";
                        this.logger.Error(nameof(MsaAccountDeleteQueueProcessor), errorMessage);
                        ev.Success = false;
                        ev.ErrorMessage = errorMessage;
                        ev.ErrorCode = $"{nameof(MsaAccountDeleteQueueProcessor)}Error";

                        // Don't exit the process, trigger it to wait and try again
                        return false;
                    }

                    // 2. Build requests, send to pcf, and complete.
                    // Each queue item at this point can be handled independent of the others.
                    string commandIds = string.Join(",", queueMessages.Where(c => c?.Data != null).Select(c => c.Data.CommandId.ToString()));
                    ev.ExtraData["CommandIds"] = commandIds;
                    AdapterResponse workerResponse = await this.BuildWriteCompleteAsync(ev, queueMessages, commandIds).ConfigureAwait(false);

                    if (!workerResponse.IsSuccess)
                    {
                        this.logger.Error(nameof(MsaAccountDeleteQueueProcessor), workerResponse.Error.Message);
                        ev.Success = false;
                        ev.ErrorMessage = workerResponse.Error.Message;
                        ev.ErrorCode = $"{nameof(MsaAccountDeleteQueueProcessor)}Error";

                        // Don't exit the process, trigger it to wait and try again
                        return false;
                    }

                    return true;
                });
        }

        private async Task<AdapterResponse> AddVerifiersAsync(IList<IQueueItem<AccountDeleteInformation>> requests)
        {
            int verifierTokenResponseErrorCount = 0;
            int invalidVerifierErrorCount = 0;

            // Try to get GDPR verifiers.
            // Note: This can be done in parallel if there is any desire to optimize (it's not currently).
            foreach (IQueueItem<AccountDeleteInformation> info in requests)
            {
                if (!string.IsNullOrWhiteSpace(info.Data.GdprVerifierToken))
                {
                    // request already has verifier token, no need to fetch one again.
                    continue;
                }

                AdapterResponse<string> verifierTokenResponse =
                    await this.GetVerifierAsync(info.Data.PreVerifierToken, info.Data.CommandId, info.Data.Xuid, info.Data.Puid)
                        .ConfigureAwait(false);

                if (!verifierTokenResponse.IsSuccess)
                {
                    this.IncrementProcessingFailure("getverifier");
                    verifierTokenResponseErrorCount++;

                    continue;
                }

                // If we got here, this call succeeded.
                info.Data.GdprVerifierToken = verifierTokenResponse.Result;

                AdapterResponse validatorResponse = await this.HasValidVerificationTokenAsync(info.Data.ToAccountCloseRequest(this.requesterId)).ConfigureAwait(false);
                if (!validatorResponse.IsSuccess)
                {
                    this.IncrementProcessingFailure("validation");
                    invalidVerifierErrorCount++;
                    continue;
                }

                this.IncrementProcessingSuccess();
            }

            var response = new AdapterResponse();

            if (verifierTokenResponseErrorCount > 0)
            {
                response.Error = new AdapterError(
                    AdapterErrorCode.Unknown,
                    $"Failed to acquire verifier tokens. Errors countered: {verifierTokenResponseErrorCount}",
                    500);
            }
            else if (invalidVerifierErrorCount > 0)
            {
                response.Error = new AdapterError(
                    AdapterErrorCode.Unknown,
                    $"Verification failed on the tokens. Errors countered: {invalidVerifierErrorCount}",
                    500);
            }

            return response;
        }

        private async Task<AdapterResponse> AddXuidsAsync(IList<IQueueItem<AccountDeleteInformation>> requests)
        {
            // Get Xuids for all accounts
            var retVal = new AdapterResponse();
            Dictionary<long, string> xuidMap;
            AdapterResponse<Dictionary<long, string>> xuidMapResponse =
                await this.xboxAccountsAdapter.GetXuidsAsync(
                    requests
                        .Where(c => !c.Data.AddXuidAttemptSucceeded)
                        .Select(c => c.Data.Puid)).ConfigureAwait(false);
            if (!xuidMapResponse.IsSuccess)
            {
                this.IncrementProcessingFailure("getxuid");
                retVal.Error = xuidMapResponse.Error;
                return retVal;
            }

            xuidMap = xuidMapResponse.Result ?? new Dictionary<long, string>();
            foreach (AccountDeleteInformation info in requests.Select(c => c.Data))
            {
                // Keep track of attempt for finding xuids in case we have a failure somewhere, no need to try again.
                // This status is only tracked if the attempt succeeded.
                info.AddXuidAttemptSucceeded = true;

                if (xuidMap.TryGetValue(info.Puid, out string xuid) && !string.Equals("0", xuid) && !string.IsNullOrWhiteSpace(xuid))
                {
                    info.Xuid = xuid;
                }
            }

            return retVal;
        }

        private async Task<AdapterResponse> BuildRequestsAsync(IList<IQueueItem<AccountDeleteInformation>> requests)
        {
            AdapterResponse addXuidResponse = await this.AddXuidsAsync(requests).ConfigureAwait(false);

            if (!addXuidResponse.IsSuccess)
            {
                return addXuidResponse;
            }

            AdapterResponse addVerifierResponse = await this.AddVerifiersAsync(requests).ConfigureAwait(false);

            if (!addVerifierResponse.IsSuccess)
            {
                return addVerifierResponse;
            }

            return new AdapterResponse();
        }

        private async Task<AdapterResponse> BuildWriteCompleteAsync(IncomingApiEventWrapper ev, IList<IQueueItem<AccountDeleteInformation>> queueMessages, string commandIds)
        {
            if (queueMessages == null)
            {
                return new AdapterResponse { Error = new AdapterError(AdapterErrorCode.InvalidInput, $"{nameof(queueMessages)} was null. This is not expected.", 500) };
            }

            // 1. Build the requests. They are missing data, such as xuids and verifier tokens which require API calls.
            AdapterResponse buildResponse = await this.BuildRequestsAsync(queueMessages).ConfigureAwait(false);

            if (!buildResponse.IsSuccess)
            {
                await this.HandleRequestBuildFailureAsync(ev, queueMessages, buildResponse);
                return buildResponse;
            }

            // 2. Write the requests - this transmits to PCF.
            AdapterResponse<IList<AccountDeleteInformation>> writeResponse =
                await this.pcfWriter.WriteDeletesAsync(queueMessages?.Select(c => c.Data).ToList(), string.Empty).ConfigureAwait(false);

            if (!writeResponse.IsSuccess)
            {
                await this.HandlePcfFailureAsync(ev, queueMessages, writeResponse);
                return writeResponse;
            }

            // 3. Complete the queue items.
            AdapterResponse completionResponse = await this.CompleteQueueProcessingAsync(queueMessages).ConfigureAwait(false);

            if (!completionResponse.IsSuccess)
            {
                ev.ErrorCode = "CompleteQueueProcessingError";
                ev.ErrorMessage = completionResponse.Error.Message;
            }

            this.logger.Verbose(
                nameof(MsaAccountDeleteQueueProcessor),
                $"Successfully completed processing {queueMessages.Count} messages. " +
                $"Command id's: {commandIds}");
            return completionResponse;
        }

        private async Task<AdapterResponse> CompleteQueueProcessingAsync(IList<IQueueItem<AccountDeleteInformation>> queueMessages)
        {
            var response = new AdapterResponse();
            var taskList = new List<Task>();
            foreach (var message in queueMessages)
            {
                taskList.Add(message.CompleteAsync());
            }

            try
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                string errorMessage = $"Failed to complete queue messages: {e}";
                this.logger.Error(nameof(MsaAccountDeleteQueueProcessor), e, errorMessage);
                response.Error = new AdapterError(AdapterErrorCode.Unknown, errorMessage, 500);
            }

            return response;
        }

        /// <summary>
        ///     Gets the verifier token.
        /// </summary>
        /// <param name="preVerifierToken">The pre verifier token that came inside the <see cref="UserDelete" /> event</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="xuid">The xuid.</param>
        /// <param name="puid">The puid.</param>
        /// <returns>The verifier token</returns>
        private async Task<AdapterResponse<string>> GetVerifierAsync(string preVerifierToken, Guid commandId, string xuid, long puid)
        {
            var verifierResponse = await this.msaIdentityServiceAdapter.GetGdprAccountCloseVerifierAsync(commandId, puid, preVerifierToken, xuid ?? string.Empty)
                .ConfigureAwait(false);
            if (!verifierResponse.IsSuccess && this.ignoreVerifierErrors)
            {
                this.logger.Warning(nameof(MsaAccountDeleteQueueProcessor), "Ignoring error from MSA SAPI: {0}", verifierResponse.Error.ToString());
                verifierResponse.Error = null;
            }

            return verifierResponse;
        }

        private async Task HandlePcfFailureAsync(
            IncomingApiEventWrapper ev,
            IList<IQueueItem<AccountDeleteInformation>> queueMessages,
            AdapterResponse<IList<AccountDeleteInformation>> writeResponse)
        {
            ev.Success = false;
            ev.ExtraData["PCF_Failure_Error_Code"] = writeResponse.Error.Code.ToString();
            ev.ExtraData["PCF_Failure_Error_Message"] = writeResponse.Error.Message;

            foreach (IQueueItem<AccountDeleteInformation> message in queueMessages)
            {
                // This updates the message content (in case other calls like getting xuid and verifier tokens were added)
                // and extends the lease
                await message.UpdateAsync(pcfFailureLeaseExtensionTime).ConfigureAwait(false);
            }
        }

        private async Task HandleRequestBuildFailureAsync(IncomingApiEventWrapper ev, IList<IQueueItem<AccountDeleteInformation>> queueMessages, AdapterResponse buildResponse)
        {
            ev.Success = false;
            ev.ExtraData["Request_Builder_Error_Code"] = buildResponse.Error.Code.ToString();
            ev.ExtraData["Request_Builder_Error_Message"] = buildResponse.Error.Message;

            foreach (IQueueItem<AccountDeleteInformation> message in queueMessages)
            {
                await message.RenewLeaseAsync(requestBuilderFailureLeaseExtensionTime).ConfigureAwait(false);
            }
        }

        private async Task<AdapterResponse> HasValidVerificationTokenAsync(PrivacyRequest arg)
        {
            var validationResponse = await this.tokenValidationService.ValidateVerifierAsync(arg, arg.VerificationToken).ConfigureAwait(false);
            if (!validationResponse.IsSuccess && this.ignoreVerifierErrors)
            {
                this.logger.Warning(nameof(MsaAccountDeleteQueueProcessor), "Ignoring error from verification: {0}", validationResponse.Error.ToString());
                validationResponse.Error = null;
            }

            return validationResponse;
        }

        /// <summary>
        ///     Increments the processing failure count by one
        /// </summary>
        /// <param name="errorId">String identifying the error</param>
        private void IncrementProcessingFailure(string errorId)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountClose, "failure", CounterType.Rate);
            counter.Increment();
            counter.Increment(errorId);
        }

        /// <summary>
        ///     Increments the processing success by one
        /// </summary>
        private void IncrementProcessingSuccess()
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.MsaAccountClose, "success", CounterType.Rate);
            counter.Increment();
        }

        private static async Task<bool> DoWorkInstrumentedAsync(IncomingApiEventWrapper apiEvent, Func<IncomingApiEventWrapper, Task<bool>> actionFunc)
        {
            try
            {
                bool success = false;
                const string StatusKey = "DoWorkStatus";
                try
                {
                    success = await actionFunc(apiEvent).ConfigureAwait(false);

                    if (success)
                    {
                        apiEvent.ExtraData[StatusKey] = true.ToString();
                    }
                    else
                    {
                        apiEvent.ExtraData[StatusKey] = false.ToString();
                    }
                }
                catch (Exception e)
                {
                    apiEvent.ErrorMessage = e.ToString();
                    apiEvent.Success = false;
                    apiEvent.RequestStatus = ServiceRequestStatus.ServiceError;
                    apiEvent.ExtraData[StatusKey] = false.ToString();
                }

                return success;
            }
            finally
            {
                apiEvent?.Finish();
            }
        }

        private static void LogDequeueCountThresholdErrors(IList<IQueueItem<AccountDeleteInformation>> queueMessages, IncomingApiEventWrapper ev)
        {
            if (queueMessages.Any(c => c.DequeueCount > DequeueCountErrorThreshold))
            {
                IList<IQueueItem<AccountDeleteInformation>> filteredQueueMessages = queueMessages.Where(c => c.DequeueCount > DequeueCountErrorThreshold).ToList();

                foreach (IQueueItem<AccountDeleteInformation> item in filteredQueueMessages)
                {
                    const string NullMessageValue = "null";
                    var errorEvent = new ErrorEvent
                    {
                        ComponentName = nameof(MsaAccountDeleteQueueProcessor),
                        ErrorCode = "DequeueCountErrorThresholdReached",
                        ErrorMessage = $"The dequeue count for this queue message has exceeded the error threshold value of {DequeueCountErrorThreshold}.",
                        ErrorDetails =
                            "TSG: search for other events for this user/command id/correlation vector and find where the failure is. " +
                            "For example, if xbox is down, for an extended period of time this error makes sense. " +
                            "However, if everything is appearing healthy, no alerts are firing, and we see this - this is abnormal and never expected to happen.",
                        ExtraData =
                        {
                            [nameof(item.Data.CorrelationVector)] = item?.Data?.CorrelationVector.ToString() ?? NullMessageValue,
                            [nameof(item.Data.CommandId)] = item?.Data?.CommandId.ToString() ?? NullMessageValue,
                            [nameof(item.DequeueCount)] = item?.DequeueCount.ToString() ?? NullMessageValue,
                            [nameof(item.Data.TimeStamp)] = item?.Data?.TimeStamp.ToString() ?? NullMessageValue,
                            [nameof(item.InsertionTime)] = item?.InsertionTime.ToString() ?? NullMessageValue,
                            [nameof(item.NextVisibleTime)] = item?.NextVisibleTime.ToString() ?? NullMessageValue,
                            [nameof(item.Id)] = item?.Id ?? NullMessageValue,
                            [nameof(item.PopReceipt)] = item?.PopReceipt ?? NullMessageValue
                        }
                    };

                    if (item?.Data != null)
                    {
                        errorEvent.Log(
                            EventLevel.Error,
                            LogOption.Realtime,
                            SllLoggingHelper.CreateUserInfo(new MsaId(item.Data.Puid)).FillEnvelope);
                    }
                    else
                    {
                        // Should always have a puid, but since this is an error log we don't want to assume anything
                        errorEvent.Log(EventLevel.Error, LogOption.Realtime);
                    }
                }

                ev.ExtraData["DequeueCountExceeded"] = string.Join(
                    ",",
                    filteredQueueMessages.Select(c => $"{c.Data.CommandId}:{c.DequeueCount}"));
            }
        }
    }
}
