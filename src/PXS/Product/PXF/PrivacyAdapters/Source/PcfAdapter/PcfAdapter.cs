// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Logging;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json;

    /// <inheritdoc />
    public class PcfAdapter : IPcfAdapter
    {
        private const string AadAuthScheme = "Bearer";

        private const string ExtraDataCommandIdsField = "CommandIds";

        private const string OperationNameForceComplete = "ForceComplete";

        private const string OperationNameGetAgentQueueStats = "GetAgentQueueStats";

        private const string OperationNameGetPcfStorageUris = "GetPcfStorageUris";

        private const string OperationNameGetRequestById = "GetRequestById";

        private const string OperationNamePostCommands = "PostCommands";

        private const string OperationNameQueryByCommandId = "QueryCommandByCommandId";

        private const string OperationNameDeleteExportArchive = "DeleteExport";

        private readonly IAadAuthManager aadAuthManager;

        private readonly IPcfPrivacyPartnerAdapterConfiguration configuration;

        private readonly ICounterFactory counterFactory;

        private readonly IHttpClient fastClient;

        private readonly IHttpClient littleSlowClient;

        private readonly IHttpClient slowClient;

        private const LogOption DefaultLogOption = LogOption.Realtime;

        public PcfAdapter(
            IPrivacyConfigurationManager configurationManager,
            IHttpClientFactory httpClientFactory,
            ICounterFactory counterFactory,
            IAadAuthManager aadAuthManager)
        {
            this.counterFactory = counterFactory;
            this.configuration = configurationManager.AdaptersConfiguration.PcfAdapterConfiguration;

            this.fastClient = this.CreateHttpClient(httpClientFactory, counterFactory);
            this.slowClient = this.CreateHttpClient(
                httpClientFactory,
                counterFactory,
                TimeSpan.FromMilliseconds(this.configuration.SlowTimeoutInMilliseconds));
            this.littleSlowClient = this.CreateHttpClient(
                httpClientFactory,
                counterFactory,
                TimeSpan.FromMilliseconds(this.configuration.LittleSlowTimeoutInMilliseconds));

            this.aadAuthManager = aadAuthManager ?? throw new ArgumentNullException(nameof(aadAuthManager));

            if (string.IsNullOrWhiteSpace(this.configuration.AadPcfTargetResource))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configurationManager.AdaptersConfiguration.PcfAdapterConfiguration.AadPcfTargetResource),
                    "AAD Target Resource is required.");
            }
        }

        public async Task<AdapterResponse> ForceCompleteAsync(Guid commandId)
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), $"debug/completecommand/{commandId}");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(this.fastClient, HttpMethod.Get, requestUri, PcfAdapter.OperationNameForceComplete)
                .ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                return new AdapterResponse
                {
                    Error = new AdapterError(
                        AdapterErrorCode.Unknown,
                        await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                        (int)responseMessage.StatusCode)
                };
            }

            return new AdapterResponse();
        }

        public async Task<AdapterResponse<AgentQueueStatisticsResponse>> GetAgentQueueStatsAsync(Guid agentId)
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), $"debug/queuestats/{agentId}");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(this.slowClient, HttpMethod.Get, requestUri, PcfAdapter.OperationNameGetAgentQueueStats)
                .ConfigureAwait(false);

            return await PcfAdapter.HandleJsonResponseAsync<AgentQueueStatisticsResponse>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<IList<Uri>>> GetPcfStorageUrisAsync()
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), "exportstorage/v1/accounts");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(this.fastClient, HttpMethod.Get, requestUri, PcfAdapter.OperationNameGetPcfStorageUris)
                .ConfigureAwait(false);

            return await PcfAdapter.HandleJsonResponseAsync<IList<Uri>>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<CommandStatusResponse>> GetRequestByIdAsync(
            Guid commandId,
            bool redacted)
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), $"debug/status/commandid/{commandId}");
            if (!redacted)
                requestUri = new Uri(new Uri(this.configuration.BaseUrl), $"coldstorage/v3/status/commandId/{commandId}");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(this.fastClient, HttpMethod.Get, requestUri, PcfAdapter.OperationNameGetRequestById)
                .ConfigureAwait(false);

            // PCF returns 204 no content, which is success, but will result in null here. This is fine.
            return await PcfAdapter.HandleJsonResponseAsync<CommandStatusResponse>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<QueryCommandByIdResult>> QueryCommandByCommandIdAsync(
            string agentId,
            string assetGroupId,
            string commandId,
            CancellationToken cancellationToken)
        {
            Uri requestUri = new Uri(
                new Uri(this.configuration.BaseUrl),
                $"coldstorage/v3/commandquery/{agentId}/{assetGroupId}/{commandId}");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(this.fastClient, HttpMethod.Get, requestUri, PcfAdapter.OperationNameQueryByCommandId)
                .ConfigureAwait(false);

            return await PcfAdapter.HandleJsonResponseAsync<QueryCommandByIdResult>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse> PostCommandsAsync(IList<PrivacyRequest> requests)
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), "pxs/commands");

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(
                    this.fastClient,
                    HttpMethod.Post,
                    requestUri,
                    PcfAdapter.OperationNamePostCommands,
                    requests,
                    CancellationToken.None,
                    e => e.ExtraData[PcfAdapter.ExtraDataCommandIdsField] = string.Join(",", requests.Select(o => o.RequestId)))
                .ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                return new AdapterResponse
                {
                    Error = new AdapterError(
                        AdapterErrorCode.Unknown,
                        await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                        (int)responseMessage.StatusCode)
                };
            }

            this.UpdateRequestRatePerformanceCounter(requests);

            // Log individual events for every data-subject-rights-request - makes it easier for Reporting
            LogDataSubjectRightsRequests(requests);

            return new AdapterResponse();
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<IList<CommandStatusResponse>>> QueryCommandStatusAsync(
            IPrivacySubject subject,
            string requester,
            IList<RequestType> requestTypes,
            DateTimeOffset oldestCommand)
        {
            var operationName = new StringBuilder("QueryCommands");
            var queryString = new StringBuilder();
            IHttpClient client = this.littleSlowClient;

            if (subject != null)
            {
                client = this.slowClient;
                operationName.Append("BySubject");
                switch (subject)
                {
                    case AadSubject aadSubject:
                        queryString.Append(queryString.Length > 0 ? "&" : string.Empty);
                        queryString.Append($"aadObjectId={aadSubject.ObjectId}");
                        break;
                    case MsaSubject msaSubject:
                        queryString.Append(queryString.Length > 0 ? "&" : string.Empty);
                        queryString.Append($"msaPuid={msaSubject.Puid}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(subject));
                }
            }

            if (requester != null)
            {
                client = this.slowClient;
                operationName.Append("ByRequester");
                queryString.Append(queryString.Length > 0 ? "&" : string.Empty);
                queryString.Append($"requester={requester}");
            }

            if (requestTypes != null && requestTypes.Count > 0)
            {
                operationName.Append("ByType");
                queryString.Append(queryString.Length > 0 ? "&" : string.Empty);
                queryString.Append("commandTypes=");
                queryString.Append(string.Join(",", requestTypes.Select(t => Enum.GetName(typeof(RequestType), t))));
            }

            if (oldestCommand != DateTimeOffset.MinValue)
            {
                operationName.Append("WithOldest");
                queryString.Append(queryString.Length > 0 ? "&" : string.Empty);
                oldestCommand = oldestCommand.ToUniversalTime();
                queryString.Append($"oldest={WebUtility.UrlEncode(oldestCommand.ToString("o"))}");
            }

            string query = queryString.Length > 0 ? $"?{queryString}" : string.Empty;
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), "coldstorage/v3/status/query" + query);

            HttpResponseMessage responseMessage = await this
                .CallPcfAsync(client, HttpMethod.Get, requestUri, operationName.ToString())
                .ConfigureAwait(false);

            return await PcfAdapter.HandleJsonResponseAsync<IList<CommandStatusResponse>>(
                    responseMessage,
                    () => new List<CommandStatusResponse>())
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Calls PCF asynchronously
        /// </summary>
        /// <param name="client">http client</param>
        /// <param name="method">http verb</param>
        /// <param name="requestUri">request URI</param>
        /// <param name="operationName">operation name</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        private async Task<HttpResponseMessage> CallPcfAsync(
            IHttpClient client,
            HttpMethod method,
            Uri requestUri,
            string operationName,
            CancellationToken? cancellationToken = null)
        {
            return await this
                .CallPcfAsync<object>(client, method, requestUri, operationName, null, cancellationToken, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Calls PCF asynchronously
        /// </summary>
        /// <typeparam name="T">type of request object to send</typeparam>
        /// <param name="client">http client</param>
        /// <param name="method">http verb</param>
        /// <param name="requestUri">request URI</param>
        /// <param name="operationName">operation name</param>
        /// <param name="postData">post data</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="apiEventAction">API event action</param>
        /// <returns>resulting value</returns>
        private async Task<HttpResponseMessage> CallPcfAsync<T>(
            IHttpClient client,
            HttpMethod method,
            Uri requestUri,
            string operationName,
            T postData,
            CancellationToken? cancellationToken = null,
            Action<OutgoingApiEventWrapper> apiEventAction = null)
            where T : class
        {
            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                operationName,
                targetUri: requestUri.ToString(),
                operationVersion: null,
                requestMethod: method,
                dependencyType: "WebService");

            apiEventAction?.Invoke(outgoingApiEvent);

            HttpRequestMessage requestMessage =
                HttpExtensions.CreateHttpRequestMessage(requestUri, method, outgoingApiEvent, null, postData);

            string accessToken =
                await this.aadAuthManager.GetAccessTokenAsync(this.configuration.AadPcfTargetResource).ConfigureAwait(false);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(PcfAdapter.AadAuthScheme, accessToken);

            return await client
                .SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken ?? CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates the HTTP client
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="timeout">timeout</param>
        /// <returns>resulting value</returns>
        private IHttpClient CreateHttpClient(
            IHttpClientFactory httpClientFactory,
            ICounterFactory counterFactory,
            TimeSpan? timeout = null)
        {
            IHttpClient client = httpClientFactory.CreateHttpClient(
                this.configuration,
                new WebRequestHandler(),
                counterFactory,
                true);

            if (timeout != null)
            {
                client.Timeout = timeout.Value;
            }

            return client;
        }

        /// <summary>
        ///     Method to expand the requests object and log the available information.
        /// </summary>
        /// <param name="request">object to expand and log</param>
        private static void CreateAndLogRequestExpansionEvent(PrivacyRequest request)
        {
            var requestExpansionEvent = new RequestExpansionEvent
            {
                RequestId = request.RequestId.ToString(),
                RequestType = request.RequestType.ToString(),
                CloudInstance = request.CloudInstance,
                Portal = request.Portal,
                HasXuid = false, // Set as default, and only override in MSA case when a Xuid is present.
                RequestGuid = request.RequestGuid.ToString()
            };

            // Build event with additional properties
            switch (request)
            {
                case DeleteRequest deleteRequest:
                    requestExpansionEvent.PrivacyDataTypes = $"{deleteRequest.PrivacyDataType}";
                    break;

                case ExportRequest exportRequest:
                    requestExpansionEvent.PrivacyDataTypes = string.Join(",", exportRequest.PrivacyDataTypes.ToArray());
                    break;
            }

            // Based on request subject, set the subject identifier in Part A and log the event.
            switch (request.Subject)
            {
                case AadSubject aadSubject:
                    requestExpansionEvent.LogInformational(
                        DefaultLogOption,
                        SllLoggingHelper.CreateUserInfo(UserIdType.AzureAdId, aadSubject.ObjectId.ToString()).FillEnvelope);
                    break;

                case DemographicSubject _:
                case MicrosoftEmployee _:
                    requestExpansionEvent.LogInformational(DefaultLogOption);
                    break;

                case DeviceSubject deviceSubject:
                    if (deviceSubject.GlobalDeviceId != default(long))
                    {
                        requestExpansionEvent.LogInformational(deviceSubject.GlobalDeviceId);
                    }
                    else if (deviceSubject.XboxConsoleId != null)
                    {
                        requestExpansionEvent.LogInformational(
                            DefaultLogOption,
                            SllLoggingHelper.CreateDeviceInfo(
                                DeviceIdType.XboxLiveHardwareId,
                                deviceSubject.XboxConsoleId.ToString()).FillEnvelope);
                    }
                    else
                    {
                        requestExpansionEvent.LogInformational(DefaultLogOption);
                    }

                    break;

                case MsaSubject msaSubject:

                    if (!string.IsNullOrWhiteSpace(msaSubject.Xuid) && 
                        !string.Equals(default(int).ToString(), msaSubject.Xuid))
                    {
                        requestExpansionEvent.HasXuid = true;
                    }

                    requestExpansionEvent.LogInformational(new MsaId(msaSubject.Puid, msaSubject.Cid));

                    break;

                default:
                    requestExpansionEvent.LogInformational(DefaultLogOption);
                    break;
            }
        }

        private void UpdateRequestRatePerformanceCounter(IList<PrivacyRequest> requests)
        {
            // Because we use a batch API for PCF, use a performance counter to count the events in the batch.
            foreach (PrivacyRequest privacyRequest in requests)
            {
                string counterInstanceNameSubjectType;
                switch (privacyRequest.Subject)
                {
                    case AadSubject _:
                        counterInstanceNameSubjectType = nameof(AadSubject);
                        break;
                    case DemographicSubject _:
                        counterInstanceNameSubjectType = nameof(DemographicSubject);
                        break;
                    case MicrosoftEmployee microsoftEmployeeSubject:
                        counterInstanceNameSubjectType = nameof(MicrosoftEmployee);
                        break;
                    case DeviceSubject _:
                        counterInstanceNameSubjectType = nameof(DeviceSubject);
                        break;
                    case MsaSubject _:
                        counterInstanceNameSubjectType = nameof(MsaSubject);
                        break;
                    default:

                        // This shouldn't throw, so just set to unknown.
                        counterInstanceNameSubjectType = "Unknown";
                        break;
                }

                string counterNameRequestType;
                switch (privacyRequest)
                {
                    case AccountCloseRequest _:
                        counterNameRequestType = nameof(AccountCloseRequest);
                        break;
                    case DeleteRequest _:
                        counterNameRequestType = nameof(DeleteRequest);
                        break;
                    case ExportRequest _:
                        counterNameRequestType = nameof(ExportRequest);
                        break;
                    default:

                        // This shouldn't throw, so just set to unknown.
                        counterNameRequestType = "Unknown";
                        break;
                }

                ICounter counter = this.counterFactory.GetCounter(
                    CounterCategoryNames.PcfAdapter, counterNameRequestType, CounterType.Rate);

                counter.Increment();
                counter.Increment(counterInstanceNameSubjectType);
            }
        }

        private static async Task<AdapterResponse<T>> HandleJsonResponseAsync<T>(
            HttpResponseMessage responseMessage,
            Func<T> defaultFactory = null)
            where T : class
        {
            if (defaultFactory == null)
            {
                defaultFactory = () => default(T);
            }

            if (!responseMessage.IsSuccessStatusCode)
            {
                return new AdapterResponse<T>
                {
                    Error = new AdapterError(
                        AdapterErrorCode.Unknown,
                        await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                        (int)responseMessage.StatusCode)
                };
            }

            return new AdapterResponse<T>
            {
                Result = JsonConvert.DeserializeObject<T>(
                             await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)) ?? defaultFactory()
            };
        }

        /// <summary>
        ///     Method to log data-subject-rights requests.
        /// </summary>
        /// <param name="requests">object to expand and log</param>
        private static void LogDataSubjectRightsRequests(IList<PrivacyRequest> requests)
        {
            foreach (PrivacyRequest request in requests)
            {
                // RequestExpansion purpose is to log expanded request information.
                // This might be use to log expanded details in an SLL log.
                CreateAndLogRequestExpansionEvent(request);

                // For all DSR requests, this log type should enable telemetry scenarios for real time views based on the SLL events.
                // May allow for creation of standing queries for meaningful pivots, ex: counts by subject, command types, request rates, etc.
                var dataSubjectRightsRequest = new DataSubjectRightsRequestExtended(request);
                dataSubjectRightsRequest.Log();
            }
        }

        /// <summary>
        /// Deletes the export archive based on given commandId
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<AdapterResponse> DeleteExportArchiveAsync(DeleteExportArchiveParameters parameters)
        {
            var requestUri = new Uri(new Uri(this.configuration.BaseUrl), "pxs/deleteexport");
            HttpResponseMessage responseMessage = await this
            .CallPcfAsync(
            this.fastClient,
            HttpMethod.Delete,
            requestUri,
            PcfAdapter.OperationNameDeleteExportArchive,
            parameters,
            CancellationToken.None)
            .ConfigureAwait(false);



            if (!responseMessage.IsSuccessStatusCode)
            {
                AdapterErrorCode errorType;
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        errorType =  AdapterErrorCode.BadRequest;
                        break;
                    case HttpStatusCode.Forbidden:
                        errorType = AdapterErrorCode.Forbidden;
                        break;
                    case HttpStatusCode.MethodNotAllowed:
                        errorType = AdapterErrorCode.MethodNotAllowed;
                        break;
                    case HttpStatusCode.NotFound:
                        errorType = AdapterErrorCode.ResourceNotFound;
                        break;
                    default:
                        errorType = AdapterErrorCode.Unknown;
                        break;
                }
                return new AdapterResponse
                {
                    Error = new AdapterError(
                errorType,
                await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                (int)responseMessage.StatusCode)
                };
            }



            return new AdapterResponse();
        }

    }
}
