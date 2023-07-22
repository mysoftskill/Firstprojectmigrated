// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Security.Cryptography.X509Certificates;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyMockService.DataSource;
    using Microsoft.Membership.MemberServices.Test.Common.DataAccess;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class PcfController : ApiController
    {
        private static ICommandStorage pcfCommandStorage;

        private readonly IValidationService validationService;

        public PcfController()
        {
            this.InitializeStorageAsync().GetAwaiter().GetResult();
            this.validationService = new ValidationService(PcvEnvironment.Preproduction);
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/debug/completecommand/{commandId}")]
        public HttpResponseMessage CompleteCommand(string commandId)
        {
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/debug/status/commandid/{commandId}")]
        public HttpResponseMessage DebugCommandId(string commandId)
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, new CommandStatusResponse());
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/exportstorage/v1/accounts")]
        public HttpResponseMessage ExportAccounts()
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, new List<Uri> { new Uri("https://pxsmock.blob.core.windows.net/") });
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/coldstorage/v3/status/commandId/{commandId}")]
        public HttpResponseMessage GetCommandIdV3(string commandId)
        {
            if (Guid.TryParse(commandId, out Guid commandIdGuid))
            {
                CommandStatusResponse cacheItem = PrivacyCommandTestCacheStorage.Instance.Get(commandIdGuid);
                return this.Request.CreateResponse(HttpStatusCode.OK, cacheItem ?? new CommandStatusResponse());
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, new CommandStatusResponse());
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/coldstorage/v3/status/query")]
        public HttpResponseMessage GetCommandsByQuery()
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, new List<CommandStatusResponse>());
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/coldstorage/v2/status/requester/{requester}")]
        public HttpResponseMessage GetCommandsByRequester(string requester)
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, new List<CommandStatusResponse>());
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/debug/queuestats/{agentId}")]
        public HttpResponseMessage QueueStats(string agentId)
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, new AgentQueueStatisticsResponse { AssetGroupQueueStatistics = new List<AssetGroupQueueStatistics>() });
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/pxs/test/commands")]
        [Description("This is a TEST API. It's used as a test hook to read from a test storage table.")]
        public async Task<HttpResponseMessage> ReadPrivacyRequestAsync(Guid objectId, Guid tenantId)
        {
            PrivacyRequest storageResult = await pcfCommandStorage.ReadPrivacyRequestFirstOrDefaultAsync(objectId, tenantId);
            if (storageResult == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, storageResult);
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/pxs/test/msa/commands")]
        [Description("This is a TEST API. It's used as a test hook to read from a test storage table.")]
        public async Task<HttpResponseMessage> ReadPrivacyRequestByMsaAsync(long msaPuid)
        {
            PrivacyRequest storageResult = await pcfCommandStorage.ReadPrivacyRequestFirstOrDefaultAsync(msaPuid);
            if (storageResult == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, storageResult);
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/pxs/test/deadletter")]
        [Description("This is a TEST API. It's used as a test hook to read from the dead letter table.")]
        public async Task<HttpResponseMessage> ReadDeadLetterItemAsync(string tenantId, string objectId)
        {
            var storageResult = await pcfCommandStorage.ReadDeadLetterItemAsync(tenantId: tenantId, objectId: objectId);
            if (storageResult == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, storageResult);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("pcf/pxs/commands")]
        public async Task<HttpResponseMessage> ReceiveEventsAsync()
        {
            string content = await this.Request.Content.ReadAsStringAsync();
            JObject[] request = JsonConvert.DeserializeObject<JObject[]>(content);

            if (request == null || request.Length == 0)
            {
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }

            var endToEndRequests = new List<PrivacyRequest>();
            var nonEndToEndRequests = new List<PrivacyRequest>();
            foreach (var req in request)
            {
                var classifiedRequest = ClassifyPrivacyRequest(req);
                if (classifiedRequest == null) continue;
                if (classifiedRequest.CorrelationVector.Contains("AidEndToEnd"))
                {
                    endToEndRequests.Add(classifiedRequest);
                }
                else
                {
                    nonEndToEndRequests.Add(classifiedRequest);
                }
            }

            // Forward PrivacyRequest to PCF for AID E2E Testing Use
            // When AnaheimIdQueueWorker calls PcfAdapter to post PrivacyRequest to PCF in ci1/ci2, requests will be routed to PXS Mock Service instead of the real PCF endpoint
            // To make E2E testing work without any changes in production code, we routes requests received from PAF.FCT to PCF here
            if (endToEndRequests.Count != 0)
            {
                await ForwardPrivacyRequetsToPCF(endToEndRequests).ConfigureAwait(false);
            }

            var insertTaskList = new List<Task<bool>>();
            foreach (PrivacyRequest privacyRequest in nonEndToEndRequests)
            {
                if (!string.IsNullOrWhiteSpace(privacyRequest.VerificationToken) && privacyRequest.Subject is MsaSubject)
                {
                    try
                    {
                        await this.validationService.EnsureValidAsync(
                            privacyRequest.VerificationToken,
                            new CommandClaims
                            {
                                CommandId = privacyRequest.RequestId.ToString(),
                                Subject = privacyRequest.Subject,
                                Operation = MapToOperation(privacyRequest.RequestType),
                                AzureBlobContainerTargetUri = (privacyRequest as ExportRequest)?.StorageUri,
                                CloudInstance = "Public",
                                ControllerApplicable = privacyRequest.ControllerApplicable,
                                ProcessorApplicable = privacyRequest.ProcessorApplicable
                            },
                            CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        return this.Request.CreateResponse(HttpStatusCode.BadRequest, $"Validation failed with exception : {e.ToString()}");
                    }
                }

                insertTaskList.Add(pcfCommandStorage.WritePrivacyRequestAsync(privacyRequest));
            }

            await Task.WhenAll(insertTaskList);

            if (insertTaskList.TrueForAll(t => t.Result))
            {
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }

            foreach (Task<bool> insertTask in insertTaskList.Where(t => t.Exception != null))
            {
                this.Logger?.Error(nameof(PcfController), insertTask.Exception, "An exception occurred while attempting to store data.");
            }

            return this.Request.CreateResponse(HttpStatusCode.InternalServerError, $"{insertTaskList.Count(t => t.Result == false)} failed to be stored.");
        }

        public PrivacyRequest ClassifyPrivacyRequest(JObject rawPxsCommand)
        {
            var request = rawPxsCommand.ToObject<PrivacyRequest>();

            if (request?.RequestType == RequestType.Delete)
            {
                return rawPxsCommand.ToObject<DeleteRequest>();
            }
            else if (request?.RequestType == RequestType.AccountClose)
            {
                return rawPxsCommand.ToObject<AccountCloseRequest>();
            }
            else if (request?.RequestType != RequestType.AgeOut)
            {
                return rawPxsCommand.ToObject<AgeOutRequest>();
            }
            else if (request?.RequestType != RequestType.Export)
            {
                return rawPxsCommand.ToObject<ExportRequest>();
            }
            else return null;
        }

        private async Task ForwardPrivacyRequetsToPCF(IList<PrivacyRequest> requests)
        {
            if (requests == null || requests.Count == 0)
                return;

            var requestUri = new Uri(new Uri(Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.BaseUrl), "pxs/commands");

            // Create the http client
            var webHandler = new WebRequestHandler();
            webHandler.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            var httpClient = new HttpClient(webHandler) { BaseAddress = requestUri };
            httpClient.Timeout = TimeSpan.FromMilliseconds(10000);

            // Build http request
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Headers.Add("X-PCF-Test-Case", "true");

            requestMessage.Content = new ObjectContent<IList<PrivacyRequest>>(requests, new JsonMediaTypeFormatter());

            try
            {
                string accessToken = await this.GetAccessTokenAsync(Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.AadPcfTargetResource).ConfigureAwait(false);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    this.Logger?.Information(nameof(PcfController), $"Forwarded PrivacyRequests with CV={requests.First().CorrelationVector} to PCF.");
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.Logger?.Error(nameof(PcfController), $"Failed to forward PrivacyRequests. StatusCode={(int)response.StatusCode}, ResponseContent={content}");
                }
            }
            catch (Exception ex)
            {
                this.Logger?.Error(nameof(PcfController), ex, $"An exception occurred while forwarding PrivacyRequests to PCF: {ex.ToString()}");
            }
        }

        private async Task<string> GetAccessTokenAsync(string resourceId)
        {
            var certFinder = new CertificateProvider(this.Logger);
            var certificate = certFinder.GetClientCertificate(Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.CertSubject, StoreLocation.LocalMachine);

            try
            {
                var tokenManager = new AadTokenManager();
                return await tokenManager.GetAppTokenAsync(
                    Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.AadAuthority,
                    Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.ClientId,
                    resourceId,
                    certificate).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                string errorMessage =
                    $"An unknown exception occurred getting the aad s2s token for {nameof(resourceId)}: {resourceId}. " +
                    $"AppId: {Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdE2EConfiguration.ClientId}. " +
                    $"Certificate Thumbprint: {certificate?.Thumbprint}";
                this.Logger?.Error(nameof(PcfController), e, errorMessage);
                throw;
            }
        }

        private ILogger Logger { get; } = DualLogger.Instance;

        private async Task InitializeStorageAsync()
        {
            if (pcfCommandStorage == null)
            {
                var pcfStorageProvider = new AzureStorageProvider(
                    this.Logger,
                    new AzureKeyVaultReader(Program.PartnerMockConfigurations, new Clock(), this.Logger));
                await pcfStorageProvider.InitializeAsync(Program.PartnerMockConfigurations.PartnerMockConfiguration.PrivacyCommandAzureStorageConfiguration)
                    .ConfigureAwait(false);
                var pcfStorageTable = new AzureTable<PrivacyCommandEntity>(
                    pcfStorageProvider,
                    this.Logger,
                    nameof(PrivacyCommandEntity).ToLowerInvariant());

                var deadLetterStorageProvider = new AzureStorageProvider(
                    this.Logger,
                    new AzureKeyVaultReader(Program.PartnerMockConfigurations, new Clock(), this.Logger));
                await deadLetterStorageProvider.InitializeAsync(Program.PartnerMockConfigurations.PartnerMockConfiguration.DeadLetterAzureStorageConfiguration)
                    .ConfigureAwait(false);
                var deadLetterTable = new AzureTable<DeadLetterStorage<AccountCloseRequest>>(
                    deadLetterStorageProvider,
                    this.Logger,
                    "accountclosedeadletterstorage");

                pcfCommandStorage = new TestCommandStorageAccess(pcfStorageTable, deadLetterTable);
            }
        }

        private static ValidOperation MapToOperation(RequestType privacyRequestRequestType)
        {
            switch (privacyRequestRequestType)
            {
                case RequestType.Delete:
                    return ValidOperation.Delete;
                case RequestType.Export:
                    return ValidOperation.Export;
                case RequestType.AccountClose:
                case RequestType.AgeOut:
                    return ValidOperation.AccountClose;
                default:
                    throw new ArgumentOutOfRangeException(nameof(privacyRequestRequestType), privacyRequestRequestType, null);
            }
        }
    }
}