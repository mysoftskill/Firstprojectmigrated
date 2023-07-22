namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.Authentication;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Client.SharedCommandFeedContracts.Partials;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Contains the default implementation of ICommandFeedClient.
    /// </summary>
    public sealed partial class CommandFeedClient : ICommandFeedClient
    {
        private static readonly ConcurrentDictionary<string, CacheItem> CachedQueueStats = new ConcurrentDictionary<string, CacheItem>();

        private const int TimeToLiveInMinutes = 15;

        private readonly IHttpClient httpClient;
        private readonly IAuthClient authClient;
        private readonly CommandFeedLogger logger;
        private readonly string clientVersion;
        private readonly Guid agentId;
        private readonly CommandFeedEndpointConfiguration endpointConfiguration;

        private readonly PcvEnvironment pcvEnvironment;
        private readonly bool enforceValidation;
        private readonly ServicePoint servicePoint;

        private IValidationService validationService;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandFeedClient"/>
        /// </summary>
        /// <param name="agentId">The agent ID (from PDMS registration).</param>
        /// <param name="authClient">The Auth Client used to get Auth Token</param>
        /// <param name="logger">The logger.</param>
        /// <param name="clientCertificate">The client certificate for MSA Auth. optional for AAD</param>
        /// <param name="factory">The http client factory, optionally.</param>
        /// <param name="endpointConfiguration">The endpoint configuration, optionally.</param>
        public CommandFeedClient(
            Guid agentId,
            IAuthClient authClient,
            CommandFeedLogger logger,
            X509Certificate2 clientCertificate = null,
            IHttpClientFactory factory = null,
            CommandFeedEndpointConfiguration endpointConfiguration = null)
            : this(agentId, clientCertificate, logger, factory, endpointConfiguration)
        {
            this.authClient = authClient;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandFeedClient"/> with MSA cert auth.
        /// </summary>
        /// <param name="agentId">The agent ID (from PDMS registration).</param>
        /// <param name="clientSiteId">The MSA site ID.</param>
        /// <param name="clientCertificate">The client certificate (with private key).</param>
        /// <param name="logger">The logger.</param>
        /// <param name="factory">The http client factory, optionally.</param>
        /// <param name="endpointConfiguration">The endpoint configuration, optionally.</param>
        public CommandFeedClient(
            Guid agentId,
            long clientSiteId,
            X509Certificate2 clientCertificate,
            CommandFeedLogger logger,
            IHttpClientFactory factory = null,
            CommandFeedEndpointConfiguration endpointConfiguration = null)
            : this(agentId, clientCertificate, logger, factory, endpointConfiguration)
        {
            this.endpointConfiguration = endpointConfiguration ?? CommandFeedEndpointConfiguration.Production;
            this.authClient = new MicrosoftAccountAuthClient(clientSiteId, logger, this.httpClient, endpointConfiguration);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandFeedClient"/> with AAD cert auth.
        /// </summary>
        /// <param name="agentId">The agent ID (from PDMS registration).</param>
        /// <param name="aadClientId">The AAD App ID.</param>
        /// <param name="clientCertificate">The client certificate (with private key).</param>
        /// <param name="logger">The logger.</param>
        /// <param name="factory">The http client factory, optionally.</param>
        /// <param name="endpointConfiguration">The endpoint configuration, optionally.</param>
        /// <param name="sendX5c">Indicates whether to send the full public key to AAD for SNI authentication.</param>
        /// <param name="azureRegion">The Azure Region to be used for auth. See https://review.docs.microsoft.com/en-us/identity/microsoft-identity-platform/msal-net-regional-adoption?branch=main for details </param>
        public CommandFeedClient(
            Guid agentId,
            string aadClientId,
            X509Certificate2 clientCertificate,
            CommandFeedLogger logger,
            IHttpClientFactory factory = null,
            CommandFeedEndpointConfiguration endpointConfiguration = null,
            bool sendX5c = true,
            string azureRegion = null)
            : this(agentId, clientCertificate, logger, factory, endpointConfiguration)
        {
            this.endpointConfiguration = endpointConfiguration ?? CommandFeedEndpointConfiguration.Production;
            this.authClient = new AzureActiveDirectoryAuthClient(aadClientId, clientCertificate, azureRegion, logger, endpointConfiguration)
            {
                UseX5cAuthentication = sendX5c
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandFeedClient"/> with AAD secret auth.
        /// For a more secure option, please consider implementing our other constructor
        /// which uses a <see cref="SecureString"/> instead of a regular string for the <paramref name="aadClientSecret"/> parameter.
        /// </summary>
        /// <param name="agentId">The agent ID (from PDMS registration).</param>
        /// <param name="aadClientId">The AAD App ID.</param>
        /// <param name="aadClientSecret">The AAD App Key used to authenticate</param>
        /// <param name="logger">The logger.</param>
        /// <param name="factory">The http client factory, optionally.</param>
        /// <param name="endpointConfiguration">The endpoint configuration, optionally.</param>
        /// <param name="azureRegion">The Azure Region to be used for auth. See https://review.docs.microsoft.com/en-us/identity/microsoft-identity-platform/msal-net-regional-adoption?branch=main for details </param>
        public CommandFeedClient(
            Guid agentId,
            string aadClientId,
            string aadClientSecret,
            CommandFeedLogger logger,
            IHttpClientFactory factory = null,
            CommandFeedEndpointConfiguration endpointConfiguration = null,
            string azureRegion = null)
            : this(agentId, null, logger, factory, endpointConfiguration)
        {
            this.endpointConfiguration = endpointConfiguration ?? CommandFeedEndpointConfiguration.Production;

            this.authClient = new AzureActiveDirectoryAuthClient(aadClientId, aadClientSecret, azureRegion, logger, endpointConfiguration);
        }

        private CommandFeedClient(
            Guid agentId,
            X509Certificate2 clientCertificate,
            CommandFeedLogger logger,
            IHttpClientFactory factory = null,
            CommandFeedEndpointConfiguration endpointConfiguration = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            factory = factory ?? new DefaultHttpClientFactory();
            this.httpClient = factory.CreateHttpClient(clientCertificate);
            this.agentId = agentId;

            this.endpointConfiguration = endpointConfiguration ?? CommandFeedEndpointConfiguration.Production;
            this.CommandsUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/commands");
            this.CheckpointUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/checkpoint");
            this.BatchCheckpointCompleteUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/batchcomplete");
            this.QueryCommandUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/command");
            this.QueueStatsUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/queuestats");
            this.ReplayUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/pcf/v1/{agentId}/replaycommands");
            this.GetBatchCommandUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/commandpages");
            this.CompleteBatchCommandUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/commandcompletions");
            this.GetAssetGroupDetailsUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/assetpages");
            this.GetResourceUriMapUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/resourceurimappages");
            this.GetCommandConfigurationUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/commandconfiguration");
            this.GetWorkitemUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/workitem");
            this.QueryWorkitemUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/workitem");
            this.UpdateWorkitemUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/workitem");
            this.GetAllBatchCommandUri = new Uri($"https://{this.endpointConfiguration.CommandFeedHostName}:443/dsr/allcommandpages");

            this.enforceValidation = this.endpointConfiguration.EnforceValidation;
            this.pcvEnvironment = this.endpointConfiguration.Environment;

            var assemblyver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            string suffix = this.endpointConfiguration.EnforceValidation ? "v:true" : "v:false";

            // A workaround to eliminate the leading zero in the file version string generated by CDPx pipeline. 
            // This is better aligned with the version we use for PCF SDK nuget package
            this.clientVersion = $"pcfsdk;{assemblyver.FileMajorPart}.{assemblyver.FileMinorPart}.{assemblyver.FileBuildPart}.{assemblyver.FilePrivatePart};{suffix}";

            // Get and configure the service point for PCF. What these settings say 
            // is that connections must not stay open more than 25 seconds, and that only
            // 100 connections may be created.
            this.servicePoint = ServicePointManager.FindServicePoint(this.CommandsUri);
            this.servicePoint.ConnectionLimit = 100;
            this.servicePoint.ConnectionLeaseTimeout = 35000;
            this.servicePoint.Expect100Continue = false;
            this.servicePoint.UseNagleAlgorithm = false;
            this.servicePoint.MaxIdleTime = 35000;
        }

        /// <summary>
        /// Gets the URI of the Checkpoint endpoint.
        /// </summary>
        public Uri CheckpointUri { get; }

        /// <summary>
        /// Gets the URI of the Batch Checkpoint complete endpoint.
        /// </summary>
        public Uri BatchCheckpointCompleteUri { get; }

        /// <summary>
        /// Gets the URI of the GetCommands endpoint.
        /// </summary>
        public Uri CommandsUri { get; }

        /// <summary>
        /// Gets the URI of the QueueStats Api endpoint.
        /// </summary>
        public Uri QueueStatsUri { get; }

        /// <summary>
        /// Gets the URI of the ReplayCommands Api endpoint.
        /// </summary>
        public Uri ReplayUri { get; }

        /// <summary>
        /// ValidationService to validate the verifier and the command.
        /// </summary>
        public IValidationService ValidationService
        {
            get => this.validationService ?? (this.validationService = new ValidationService(this.pcvEnvironment));

            set => this.validationService = value;
        }

        /// <summary>
        /// Gets the URI of the QueryCommand endpoint
        /// </summary>
        public Uri QueryCommandUri { get; }

        /// <inheritdoc />
        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations
        {
            get => this.ValidationService.SovereignCloudConfigurations;

            set => this.ValidationService.SovereignCloudConfigurations = value;
        }

        /// <summary>
        /// The amount of time for which leases are acquired.
        /// Please set a value between 15 minutes and a day.
        /// </summary>
        public TimeSpan? RequestedLeaseDuration { get; set; }

        /// <summary>
        /// Asynchronously fetches the next batch of commands.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of commands.</returns>
        public async Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, this.CommandsUri);
            await this.AddCommonHeadersAsync(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);
            this.logger.HttpResponseReceived(getRequest, response);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new List<IPrivacyCommand>();
            }

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetCommands returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<CommandFeedContracts.GetCommandsResponse>(responseBody);

            List<PrivacyCommand> commands = new List<PrivacyCommand>(parsedResponse);
            List<IPrivacyCommand> verifiedCommands = new List<IPrivacyCommand>();
            foreach (var command in commands)
            {
                try
                {
                    await this.ValidateCommandAsync(command, cancellationToken).ConfigureAwait(false);
                    command.CheckpointCallback = this.CheckpointAsync;

                    // strip the verifier to prevent accidental storage by the agents
                    command.Verifier = string.Empty;
                    verifiedCommands.Add(command);
                }
                catch (InvalidOperationException ex)
                {
                    this.logger.CommandValidationException(command.CorrelationVector, command.CommandId, ex);
                    await this.CheckpointAsync(command.CommandId, command.AgentState, CommandStatus.UnexpectedVerificationFailure, 0, command.LeaseReceipt).ConfigureAwait(false);
                }
                catch (InvalidPrivacyCommandException ex)
                {
                    this.logger.CommandValidationException(command.CorrelationVector, command.CommandId, ex);
                    await this.CheckpointAsync(command.CommandId, command.AgentState, CommandStatus.VerificationFailed, 0, command.LeaseReceipt).ConfigureAwait(false);
                }
                catch (KeyDiscoveryException ex)
                {
                    this.logger.CommandValidationException(command.CorrelationVector, command.CommandId, ex);
                    await this.CheckpointAsync(command.CommandId, command.AgentState, CommandStatus.UnexpectedVerificationFailure, 0, command.LeaseReceipt).ConfigureAwait(false);
                }
            }

            return verifiedCommands;
        }

        /// <summary>
        /// Checkpoints the command and returns a new lease receipt.
        /// </summary>
        public async Task<string> CheckpointAsync(
            string commandId,
            string agentState,
            CommandStatus commandStatus,
            int affectedRowCount,
            string leaseReceipt,
            TimeSpan? leaseExtension = null,
            IEnumerable<string> variantIds = null,
            IEnumerable<string> nonTransientFailures = null,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            var request = new CommandFeedContracts.CheckpointRequest
            {
                AgentState = agentState,
                CommandId = commandId,
                LeaseExtensionSeconds = (int)(leaseExtension?.TotalSeconds ?? 0),
                LeaseReceipt = leaseReceipt,
                RowCount = affectedRowCount,
                Status = commandStatus.ToString(),
                Variants = variantIds?.ToArray(),
                NonTransientFailures = nonTransientFailures?.ToArray(),
                ExportedFileSizeDetails = exportedFileSizeDetails?.ToList()
            };

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, this.CheckpointUri);
            postRequest.Content = new StringContent(
                JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(postRequest, CancellationToken.None).ConfigureAwait(false);
            this.logger.HttpResponseReceived(postRequest, response);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new CheckpointConflictException($"Checkpoint Conflict for command: {commandId}");
            }

            string responseBody = string.Empty;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.Checkpoint returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var checkpointResponse = JsonConvert.DeserializeObject<CheckpointResponse>(responseBody);
            return checkpointResponse.LeaseReceipt;
        }

        /// <summary>
        /// Completes a batch of up to 100 commands at a time.
        /// </summary>
        /// <param name="processedCommands">The commands ready for checkpoint complete.</param>
        /// <returns>Returns if operation is successful or if the collection is empty.</returns>
        /// <exception cref="ArgumentNullException">If the collection is null.</exception>
        /// <exception cref="HttpRequestException">If this request fails. Errors logged to <see cref="CommandFeedLogger"/> BatchCompleteError.</exception>
        public async Task BatchCheckpointCompleteAsync(IEnumerable<ProcessedCommand> processedCommands)
        {
            if (processedCommands == null)
            {
                throw new ArgumentNullException(nameof(processedCommands));
            }

            processedCommands = processedCommands.ToArray();

            if (!processedCommands.Any())
            {
                return;
            }

            var postRequest = new HttpRequestMessage(HttpMethod.Post, this.BatchCheckpointCompleteUri);
            IEnumerable<CheckpointCompleteRequest> payload = processedCommands.Select(ProcessedCommand.ToCheckpointCompleteRequest);
            postRequest.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);
            HttpResponseMessage response = await this.httpClient.SendAsync(postRequest, CancellationToken.None).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return; // Successfully processed all commands!
            }

            string responseBody = string.Empty;
            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                {
                    failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
                }
            }

            // The request failed
            if (response.StatusCode == HttpStatusCode.BadRequest && failedCheckpoints != null)
            {
                foreach (CheckpointCompleteResponse failed in failedCheckpoints)
                {
                    this.logger.BatchCompleteError(failed.CommandId, failed.Error);
                }
            }

            throw CreateException(
                $"CommandFeed.BatchCheckpointComplete wasn't successful: Status Code = \"{response.StatusCode}\", Body = \"{responseBody}\".",
                response);
        }

        /// <summary>
        /// Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <returns>The full command</returns>
        public async Task<IPrivacyCommand> QueryCommandAsync(string leaseReceipt, CancellationToken cancellationToken)
        {
            var request = new CommandFeedContracts.QueryCommandRequest
            {
                LeaseReceipt = leaseReceipt
            };

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, this.QueryCommandUri);
            postRequest.Content = new StringContent(
                JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(postRequest, cancellationToken).ConfigureAwait(false);
            this.logger.HttpResponseReceived(postRequest, response);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            string responseBody = await response.Content?.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.QueryCommand returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var queryCommandResponse = JsonConvert.DeserializeObject<CommandFeedContracts.QueryCommandResponse>(responseBody);
            var command = PrivacyCommandFeedParser.ParseObject(queryCommandResponse.Command);
            command.CheckpointCallback = this.CheckpointAsync;

            return command;
        }

        /// <summary>
        /// A test-only method to insert a set of commands into the command feed. This API is intended for use for integration and testing
        /// in non-production environments. In production, PCF will categorically reject these requests.
        /// </summary>
        public async Task InsertCommandsAsync(IEnumerable<PrivacyCommand> commands, CancellationToken cancellationToken)
        {
            List<JObject> jsonCommands = new List<JObject>();
            foreach (var command in commands)
            {
                jsonCommands.Add(JObject.FromObject(command));
            }

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, this.CommandsUri);
            postRequest.Content = new StringContent(
                JsonConvert.SerializeObject(jsonCommands), Encoding.UTF8, "application/json");

            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(postRequest, cancellationToken).ConfigureAwait(false);
            this.logger.HttpResponseReceived(postRequest, response);

            string responseBody = string.Empty;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw CreateException($"CommandFeed.Insert returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }
        }

        /// <summary>
        /// This is in BETA Testing, 
        /// please reach out to ngppoeng to have your agentId enabled to call this Api
        /// Gets the stats on the agent's queue depth
        /// </summary>
        /// <param name="assetGroupQualifier">
        ///     AssetGroupQualifier to filter the stats on, all assetgroups of the agent if null
        /// </param>
        /// <param name="commandType">PrivacyCommandType: AccoutClose, Delete, or Export to optionally filter on</param>
        /// <returns>A list of queueStats</returns>
        public async Task<List<QueueStats>> GetQueueStatsAsync(
            string assetGroupQualifier = null,
            string commandType = null)
        {
            // first try to get from cache, if found and not stale
            string cacheKey = GetCacheKey(assetGroupQualifier, commandType);
            if (CachedQueueStats.TryGetValue(cacheKey, out CacheItem cacheItem) 
                && cacheItem.Expiration.CompareTo(DateTimeOffset.UtcNow) > 0)
            {
                return cacheItem.QueueStats;
            }

            var request = new QueueStatsRequest
            {
                AssetGroupQualifier = assetGroupQualifier,
                CommandType = commandType
            };

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, this.QueueStatsUri);
            postRequest.Content = new StringContent(
                JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(postRequest, CancellationToken.None).ConfigureAwait(false);
            this.logger.HttpResponseReceived(postRequest, response);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            string responseBody = string.Empty;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.QueueStats returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var queueStatsResponse = JsonConvert.DeserializeObject<QueueStatsResponse>(responseBody);

            // cache the results
            CachedQueueStats[cacheKey] = new CacheItem(queueStatsResponse.QueueStats, TimeToLiveInMinutes);

            return queueStatsResponse.QueueStats;
        }

        /// <summary>
        /// Replay commands by a list of Command Ids.
        /// </summary>
        /// <param name="commandIds">The commands that needs to be replayed</param>
        /// <param name="assetGroupQualifiers">The specific asset groups that the commands should be replayed for. All asset groups of the agent if null</param>
        /// <exception cref="ArgumentException">If commandIds is null or empty.</exception>
        public async Task ReplayCommandsByIdAsync(IEnumerable<string> commandIds, IEnumerable<string> assetGroupQualifiers = null)
        {
            if (commandIds == null || commandIds.Count() < 1)
            {
                throw new ArgumentException(nameof(commandIds));
            }

            var replayRequest = new ReplayCommandsRequest
            {
                CommandIds = commandIds.ToArray(),
                AssetQualifiers = assetGroupQualifiers?.ToArray()
            };

            await this.ReplayCommandsAsync(replayRequest);
        }

        /// <summary>
        /// Replay commands for specific dates.
        /// </summary>
        /// <param name="replayFromDate">Replay all commands from this date. Current replay granularity is a full date.</param>
        /// <param name="replayToDate">Replay all commands to this date, inclusive. Current replay granularity is a full date.</param>
        /// <param name="assetGroupQualifiers">The specific asset groups that the commands should be replayed for. All asset groups of the agent if null</param>
        public async Task ReplayCommandsByDatesAsync(DateTimeOffset replayFromDate, DateTimeOffset replayToDate, IEnumerable<string> assetGroupQualifiers = null)
        {
            var replayRequest = new ReplayCommandsRequest
            {
                ReplayFromDate = replayFromDate,
                ReplayToDate = replayToDate,
                AssetQualifiers = assetGroupQualifiers?.ToArray()
            };

            await this.ReplayCommandsAsync(replayRequest);
        }

        private async Task ReplayCommandsAsync(ReplayCommandsRequest replayRequest)
        {
            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, this.ReplayUri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(replayRequest), Encoding.UTF8, "application/json")
            };

            await this.AddCommonHeadersAsync(postRequest).ConfigureAwait(false);
            var response = await this.httpClient.SendAsync(postRequest, CancellationToken.None).ConfigureAwait(false);
            this.logger.HttpResponseReceived(postRequest, response);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            string responseBody = string.Empty;

            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            throw CreateException($"CommandFeed.ReplayCommands returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
        }

        private async Task AddCommonHeadersAsync(HttpRequestMessage request)
        {
            var token = await this.authClient.GetAccessTokenAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(this.authClient.Scheme, token);

            request.Headers.Add("x-client-version", this.clientVersion);
            request.Headers.Add("x-supported-commands", "AccountClose,Delete,Export");

            if (this.RequestedLeaseDuration.HasValue)
            {
                request.Headers.Add("x-lease-duration-seconds", this.RequestedLeaseDuration.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static HttpRequestException CreateException(string message, HttpResponseMessage response)
        {
            var ex = new HttpRequestException(message);
            ex.Data.Add("StatusCode", response.StatusCode);
            return ex;
        }

        private async Task ValidateCommandAsync(IPrivacyCommand command, CancellationToken cancellationToken)
        {
            if (this.enforceValidation || !string.IsNullOrEmpty(command.Verifier))
            {
                ValidOperation operation = new ValidatorVisitor(this.logger).Visit(command);
                Uri azureBlobContainerTargetUri = null;
                DataTypeId deleteCommandDataType = null;

                if (command.GetType() == typeof(ExportCommand))
                {
                    azureBlobContainerTargetUri = (command as ExportCommand).AzureBlobContainerTargetUri;
                }
                else if (command.GetType() == typeof(DeleteCommand))
                {
                    deleteCommandDataType = (command as DeleteCommand).PrivacyDataType;
                }
                else if (command.GetType() == typeof(AccountCloseCommand))
                {
                    if (string.IsNullOrEmpty(command.Verifier))
                    {
                        // Log command detail when verifier is null
                        this.logger.CommandValidationException(command.CorrelationVector, command.CommandId, new ArgumentNullException(JsonConvert.SerializeObject(command)));
                    }
                }

                await this.ValidationService.EnsureValidAsync(
                    command.Verifier,
                    new CommandClaims
                    {
                        CommandId = command.CommandId,
                        Subject = command.Subject,
                        Operation = operation,
                        AzureBlobContainerTargetUri = azureBlobContainerTargetUri,
                        CloudInstance = command.CloudInstance,
                        ControllerApplicable = command.ControllerApplicable,
                        ProcessorApplicable = command.ProcessorApplicable,
                        DataType = deleteCommandDataType
                    },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static string GetCacheKey(string assetQualifier, string commandType)
        {
            string key = string.Empty;
            if (!string.IsNullOrWhiteSpace(assetQualifier))
            {
                key = assetQualifier + "|";
            }

            if (!string.IsNullOrWhiteSpace(commandType))
            {
                key += commandType;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = "all";
            }

            return key;
        }

        internal class CacheItem
        {
            public List<QueueStats> QueueStats { get; set; }

            public DateTimeOffset Expiration { get; set; }

            public CacheItem(List<QueueStats> queueStats, int timeToLiveInMinutes)
            {
                this.QueueStats = queueStats;
                this.Expiration = DateTimeOffset.UtcNow.AddMinutes(timeToLiveInMinutes);
            }
        }
    }
}
