namespace Microsoft.PrivacyServices.AzureFunctions.AIdFunctionalTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.PrivacyServices.AnaheimId;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Newtonsoft.Json;
    using OSGSHttpClient = Microsoft.OSGS.HttpClientCommon;
    using PCFHttpClient = Microsoft.PrivacyServices.CommandFeed.Client;


    /// <summary>
    /// Hosting environment.
    /// </summary>
    public static class AidTestHelpers
    {
        /// <summary>
        /// AgentId used for AId E2E Testing
        /// </summary>
        public static readonly Guid AIdAgentId = Guid.Parse("db99e11a06a7403f8cf1c5bfd79f95af");

        /// <summary>
        /// Test MSA Site ID
        /// </summary>
        public const long TestSiteId = 296170;

        private const string MsaTicketUri = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf";

        private const string PcfMsaSiteName = "pcf.privacy.microsoft-int.com";

        private const string AadAuthority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";

        private const string PcfAadResourceId = "https://pcf.privacy.microsoft-int.com";

        /// <summary>
        /// dummy logger used in PCF
        /// </summary>
        public static CommandFeedLogger DummyPcfLogger;

        static AidTestHelpers()
        {
            DummyPcfLogger = new NullCommandFeedLogger();
        }

        /// <summary>
        /// Get Deployment Environment
        /// </summary>
        /// <returns>DeploymentEnvironment</returns>
        public static DeploymentEnvironment GetDeploymentEnvironment()
        {
            DeploymentEnvironment deploymentEnvironment = DeploymentEnvironment.ONEBOX;
            string env = Environment.GetEnvironmentVariable("PAF_TestEnvironmentName", EnvironmentVariableTarget.Process);

            // PAF_TestEnvironmentName should not be present on OneBox
            if (!string.IsNullOrEmpty(env))
            {
                if (!Enum.TryParse(env, out deploymentEnvironment))
                {
                    throw new ArgumentOutOfRangeException($"Cannot parse {env} to {nameof(DeploymentEnvironment)}.");
                }
            }

            return deploymentEnvironment;
        }

        /// <summary>
        /// Create queue client.
        /// </summary>
        /// <param name="queueAccountInfo">Account info.</param>
        /// <param name="aidFctConfig">Config.</param>
        /// <typeparam name="T">Message type.</typeparam>
        /// <returns>IBasicCloudQueueClient</returns>
        public static ICloudQueue<T> CreateCloudQueueClient<T>(QueueAccountInfo queueAccountInfo, AidFctConfig aidFctConfig)
        {
            // If OneBox then storage emulator
            if (aidFctConfig.DeploymentEnvironment == DeploymentEnvironment.ONEBOX)
            {
                return new CloudQueue<T>(queueAccountInfo.QueueName, aidFctConfig.MessageEncoding);
            }

            return new CloudQueue<T>(
                accountName: queueAccountInfo.StorageAccountName,
                queueName: queueAccountInfo.QueueName,
                GetClientCredentials(aidFctConfig),
                aidFctConfig.MessageEncoding);
        }

        /// <summary>
        /// Get client credentials.
        /// </summary>
        /// <param name="aidFctConfig">Config.</param>
        /// <returns>Client credentials.</returns>
        public static TokenCredential GetClientCredentials(AidFctConfig aidFctConfig)
        {
            var cert = CertificateFinder.FindCertificateByName(aidFctConfig.CloudTestCertSubjectName);

            // SN/I Client credentials
            return new ConfidentialCredential(
                    aidFctConfig.AmeTenantId,
                    aidFctConfig.AidClientId,
                    cert);
        }

        /// <summary>
        ///     Sends a device delete request for a given device id, correlation vector, and user id.
        /// </summary>
        /// <param name="testHttpClient">a test http client</param>
        /// <param name="deviceIdString">device id string</param>
        /// <param name="correlationVector">cv</param>
        /// <param name="userId">user id</param>
        /// <returns>Task of HttpResponseMessage</returns>
        public static async Task<HttpResponseMessage> SendDeviceDeleteRequest(OSGSHttpClient.IHttpClient testHttpClient, string deviceIdString, string correlationVector, string userId)
        {
            string jsonEvents = CreateJsonVortexEvents(correlationVector, deviceIdString, userId);
            HttpRequestMessage request = await CreateDeviceDeleteRequestAsync(jsonEvents);
            return await testHttpClient.SendAsync(request).ConfigureAwait(false); ;
        }

        /// <summary>
        ///     Creates a device delete request message.
        /// </summary>
        /// <param name="jsonEvents">string containing all vortex events in json format</param>
        /// <returns>Task of HttpRequestMessage</returns>
        private static async Task<HttpRequestMessage> CreateDeviceDeleteRequestAsync(string jsonEvents)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post
            };
            string VortexIngestionDeviceDeleteV1 = "v1/vortex/devicedelete";
            request.RequestUri = new Uri(VortexIngestionDeviceDeleteV1, UriKind.Relative);
            request.Properties.Add("OperationNameKey", "diagdelete");
            string ClientRequestId = "client-request-id";
            request.Headers.Add(ClientRequestId, Guid.NewGuid().ToString());
            string VortexServedBy = "X-Served-By";
            request.Headers.Add(VortexServedBy, "functestserver");
            string WatchdogRequest = "X-PXS-WATCHDOG";
            request.Headers.Add(WatchdogRequest, new[] { true.ToString() });
            HttpContent contents = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonEvents));
            request.Content = await contents.CompressGZip().ConfigureAwait(false);
            return request;
        }

        /// <summary>
        ///     Creates a json list of vortex events
        /// </summary>
        /// <param name="correlationVector">unique identifier for requests</param>
        /// <param name="deviceId">device id for the event</param>
        /// <param name="userId">user id for the event</param>
        /// <returns>String of vortex events</returns>
        private static string CreateJsonVortexEvents(string correlationVector, string deviceId, string userId)
        {
            var vortexEevent = new VortexEvent
            {
                Time = DateTimeOffset.UtcNow,
                CorrelationVector = correlationVector,
                Ext = new VortexEvent.Extensions
                {
                    Device = new VortexEvent.Device
                    {
                        Id = deviceId,
                    },
                    User = new VortexEvent.User
                    {
                        Id = userId,
                    },
                },
                Data = new VortexEvent.VortexData
                {
                    IsInitiatedByUser = RandomHelper.Next(0, 2),
                }
            };

            var vortexEevents = new VortexEvents
            {
                Events = new[] { vortexEevent }
            };

            return JsonConvert.SerializeObject(vortexEevents);
        }

        /// <summary>
        ///  Try to poll as many commands as possible in given time range
        /// </summary>
        public static async Task<List<PrivacyCommand>> PollToReceiveCommandsAsync(string getCommandsUri, string e2eCV, int maxPollRetry, PCFHttpClient.IHttpClient httpClient, MicrosoftAccountAuthClient authClient)
        {
            List<PrivacyCommand> allCommands = new List<PrivacyCommand>();

            while (maxPollRetry-- > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, getCommandsUri);
                await AddCommonHeadersAsync(authClient, getRequest).ConfigureAwait(false);
                var response = await httpClient.SendAsync(getRequest, CancellationToken.None).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = JsonConvert.DeserializeObject<GetCommandsResponse>(responseBody);
                List<PrivacyCommand> receivedCommands = new List<PrivacyCommand>(parsedResponse);
                Console.WriteLine($"{nameof(PollToReceiveCommandsAsync)}: Received {receivedCommands.Count} commands.");

                allCommands.AddRange(receivedCommands);
            }
            return allCommands;
        }

        /// <summary>
        /// Checkpoints the command and returns a new lease receipt.
        /// </summary>
        public static async Task CheckPointCommandAsync(
            string checkpointUri,
            PrivacyCommand command,
            CommandStatus commandStatus,
            int affectedRowCount,
            IHttpClient httpClient,
            MicrosoftAccountAuthClient authClient)
        {
            var request = new CheckpointRequest
            {
                AgentState = command.AgentState,
                CommandId = command.CommandId,
                LeaseExtensionSeconds = 0,
                LeaseReceipt = command.LeaseReceipt,
                RowCount = affectedRowCount,
                Status = commandStatus.ToString(),
                Variants = null,
                NonTransientFailures = null,
                ExportedFileSizeDetails = null
            };

            var url = new Uri(checkpointUri);
            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            };

            await AidTestHelpers.AddCommonHeadersAsync(authClient, postRequest).ConfigureAwait(false);

            var response = await httpClient.SendAsync(postRequest, CancellationToken.None).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"{nameof(CheckPointCommandAsync)}: TimeStamp={DateTimeOffset.UtcNow}. Checkpoint command:{command.CommandId} successfully.");
            }
            else
            {
                Console.WriteLine($"{nameof(CheckPointCommandAsync)}: Checkpoint failed for command:{command.CommandId}. StatusCode={response.StatusCode}");
            }

            string responseBody = string.Empty;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            var checkpointResponse = JsonConvert.DeserializeObject<CheckpointResponse>(responseBody);
            command.LeaseReceipt = checkpointResponse.LeaseReceipt;
        }

        /// <summary>
        /// Gets the command feed client configuration.
        /// </summary>
        public static CommandFeedEndpointConfiguration CreateTestEndpointConfig(AidFctConfig aidFctConfig)
        {
            return new CommandFeedEndpointConfiguration(
                    new Uri(MsaTicketUri),
                    PcfMsaSiteName,
                    AadAuthority,
                    PcfAadResourceId,
                    aidFctConfig.PcfApiHost,
                    PcvEnvironment.Preproduction,
                    false);
        }

        /// <summary>
        /// Gets the command feed client configuration.
        /// </summary>
        public static async Task AddCommonHeadersAsync(IAuthClient authClient, HttpRequestMessage request)
        {
            var token = await authClient.GetAccessTokenAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(authClient.Scheme, token);

            request.Headers.Add("x-client-version", $"pcfsdk;1.0.0.0");
            request.Headers.Add("x-supported-commands", "AccountClose,Delete,Export");

            var requestedLeaseDuration = TimeSpan.FromSeconds(3600);

            request.Headers.Add("x-lease-duration-seconds", requestedLeaseDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        private class NullCommandFeedLogger : CommandFeedLogger
        {
            public override void UnhandledException(Exception ex)
            {
            }
        }
    }
}
