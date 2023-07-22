// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.PcfAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class PcfAdapterTests
    {
        private readonly Policy policy = Policies.Current;

        private Mock<IAadAuthManager> authManager;

        private Mock<IPcfPrivacyPartnerAdapterConfiguration> configuration;

        private Mock<ICounter> counter;

        private Mock<ICounterFactory> counterFactory;

        private Mock<IHttpClient> httpClient;

        private IPcfAdapter pcfAdapter;

        private HttpResponseMessage response;

        [TestInitialize]
        public void Initialize()
        {
            this.configuration = new Mock<IPcfPrivacyPartnerAdapterConfiguration>();
            this.configuration.SetupGet(c => c.BaseUrl).Returns("https://doesnotmatter.com");
            this.configuration.SetupGet(c => c.PartnerId).Returns("doesnotmatter");
            this.configuration.SetupGet(c => c.AadPcfTargetResource).Returns("https://MSAzureCloud.onmicrosoft.com/469dcb1e-f765-4199-b091-1907c74d8a22");

            Mock<IAdaptersConfiguration> adapterConfig = new Mock<IAdaptersConfiguration>();
            adapterConfig.SetupGet(c => c.PcfAdapterConfiguration).Returns(this.configuration.Object);

            Mock<IPrivacyConfigurationManager> configManager = new Mock<IPrivacyConfigurationManager>();
            configManager.SetupGet(m => m.AdaptersConfiguration).Returns(adapterConfig.Object);

            this.httpClient = new Mock<IHttpClient>();

            Mock<IHttpClientFactory> clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(
                    f => f.CreateHttpClient(
                        It.IsAny<IPrivacyPartnerAdapterConfiguration>(), 
                        It.IsAny<WebRequestHandler>(), 
                        It.IsAny<ICounterFactory>(), 
                        It.IsAny<bool>()))
                .Returns(this.httpClient.Object);

            this.response = new HttpResponseMessage(HttpStatusCode.OK);

            this.httpClient
                .Setup(
                    m => m.SendAsync(
                        It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.response);

            this.authManager = new Mock<IAadAuthManager>();
            this.authManager.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync("I'manaccesstoken");

            this.counterFactory = new Mock<ICounterFactory>();

            this.counter = new Mock<ICounter>();

            this.counterFactory
                .Setup(x => x.GetCounter(It.IsAny<string>(), It.IsAny<string>(), CounterType.Rate))
                .Returns(this.counter.Object);

            this.pcfAdapter = new PcfAdapter(
                configManager.Object,
                clientFactory.Object,
                this.counterFactory.Object,
                this.authManager.Object);
        }

        [TestMethod]
        public async Task ForceCompleteAsyncOfFail()
        {
            Guid id = Guid.Empty;
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.ForceCompleteAsync(id).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
        }

        [TestMethod]
        public async Task ForceCompleteAsyncOfSuccess()
        {
            Guid id = Guid.NewGuid();
            AdapterResponse actualResult = await this.pcfAdapter.ForceCompleteAsync(id).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        public async Task GetAgentQueueStatsAsyncOfFail()
        {
            Guid id = Guid.Empty;
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.GetAgentQueueStatsAsync(id).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
        }

        [TestMethod]
        public async Task GetAgentQueueStatsAsyncOfSuccess()
        {
            AgentQueueStatisticsResponse agentQueueResponse = this.GenerateAgentQueueStatisticsResponse();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(agentQueueResponse));

            AdapterResponse<AgentQueueStatisticsResponse> actualResult = await this.pcfAdapter.GetAgentQueueStatsAsync(Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(1, actualResult.Result.AssetGroupQueueStatistics.Count);
            Assert.AreEqual(agentQueueResponse.AssetGroupQueueStatistics[0].SubjectType, actualResult.Result.AssetGroupQueueStatistics[0].SubjectType);
            Assert.AreEqual(agentQueueResponse.AssetGroupQueueStatistics[0].PendingCommandCount, actualResult.Result.AssetGroupQueueStatistics[0].PendingCommandCount);
            Assert.AreEqual(agentQueueResponse.AssetGroupQueueStatistics[0].UnleasedCommandCount, actualResult.Result.AssetGroupQueueStatistics[0].UnleasedCommandCount);
            Assert.AreEqual(agentQueueResponse.AssetGroupQueueStatistics[0].AgentId, actualResult.Result.AssetGroupQueueStatistics[0].AgentId);
            Assert.AreEqual(agentQueueResponse.AssetGroupQueueStatistics[0].AssetGroupId, actualResult.Result.AssetGroupQueueStatistics[0].AssetGroupId);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public async Task GetPcfStorageUrisAsyncFail()
        {
            AdapterResponse<IList<Uri>> actualResult = await this.pcfAdapter.GetPcfStorageUrisAsync().ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Result);
        }

        [TestMethod]
        public async Task GetPcfStorageUrisAsyncSuccess()
        {
            var uriList = new List<Uri>
            {
                new Uri("https://doesnotmatter.com"),
                new Uri("https://doesnotmatter1.com")
            };
            string contentText = JsonConvert.SerializeObject(uriList);

            this.response.Content = new StringContent(contentText);
            AdapterResponse<IList<Uri>> actualResult = await this.pcfAdapter.GetPcfStorageUrisAsync().ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(uriList.Count, actualResult.Result.Count);
            Assert.AreEqual(uriList[0], actualResult.Result[0]);
            Assert.AreEqual(uriList[1], actualResult.Result[1]);
        }

        [TestMethod]
        public async Task GetRequestByIdAsyncWithGuidFail()
        {
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.GetRequestByIdAsync(Guid.NewGuid(), false).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
        }

        [TestMethod]
        public async Task GetRequestByIdAsyncWithGuidSuccess()
        {
            CommandStatusResponse commandStatusResponse = this.GenerateCommandStatusResponse();

            this.response.Content =
                new StringContent(JsonConvert.SerializeObject(commandStatusResponse));
            AdapterResponse<CommandStatusResponse> actualResult = await this.pcfAdapter.GetRequestByIdAsync(Guid.NewGuid(), true).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(commandStatusResponse.Context, actualResult.Result.Context);
            Assert.AreEqual(commandStatusResponse.CommandId, actualResult.Result.CommandId);
            Assert.AreEqual(commandStatusResponse.TotalCommandCount, actualResult.Result.TotalCommandCount);
            Assert.AreEqual(commandStatusResponse.CommandType, actualResult.Result.CommandType);
            Assert.AreEqual(commandStatusResponse.SubjectType, actualResult.Result.SubjectType);
            Assert.AreEqual(commandStatusResponse.Requester, actualResult.Result.Requester);
            Assert.AreEqual(commandStatusResponse.CommandType, actualResult.Result.CommandType);
            Assert.AreEqual(commandStatusResponse.CompletedTime, actualResult.Result.CompletedTime);
            Assert.AreEqual(commandStatusResponse.CompletionSuccessRate, actualResult.Result.CompletionSuccessRate);
            Assert.AreEqual(commandStatusResponse.CreatedTime, actualResult.Result.CreatedTime);
        }

        [TestMethod]
        public async Task GetRequestByIdAsyncWithoutGuidFail()
        {
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.GetRequestByIdAsync(Guid.Empty, false).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
        }

        [TestMethod]
        public async Task GetRequestByIdAsyncWithoutGuidSuccess()
        {
            CommandStatusResponse commandStatusResponse = this.GenerateCommandStatusResponse();
            this.response.Content =
                new StringContent(JsonConvert.SerializeObject(commandStatusResponse));
            AdapterResponse<CommandStatusResponse> actualResult = await this.pcfAdapter.GetRequestByIdAsync(Guid.Empty, true).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(commandStatusResponse.Context, actualResult.Result.Context);
            Assert.AreEqual(commandStatusResponse.CommandId, actualResult.Result.CommandId);
            Assert.AreEqual(commandStatusResponse.TotalCommandCount, actualResult.Result.TotalCommandCount);
            Assert.AreEqual(commandStatusResponse.CommandType, actualResult.Result.CommandType);
            Assert.AreEqual(commandStatusResponse.SubjectType, actualResult.Result.SubjectType);
            Assert.AreEqual(commandStatusResponse.Requester, actualResult.Result.Requester);
            Assert.AreEqual(commandStatusResponse.CommandType, actualResult.Result.CommandType);
            Assert.AreEqual(commandStatusResponse.CompletedTime, actualResult.Result.CompletedTime);
            Assert.AreEqual(commandStatusResponse.CompletionSuccessRate, actualResult.Result.CompletionSuccessRate);
            Assert.AreEqual(commandStatusResponse.CreatedTime, actualResult.Result.CreatedTime);
        }

        [TestMethod]
        public async Task PostCommandsAsyncAccountCloseRequestFail()
        {
            AccountCloseRequest acountCloseRequest = new AccountCloseRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.AccountClose
            };
            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(acountCloseRequest);

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task PostCommandsAsyncAccountCloseRequestSuccess()
        {
            AccountCloseRequest acountCloseRequest = new AccountCloseRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.AccountClose
            };
            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(acountCloseRequest);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);
            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        public async Task PostCommandsAsyncDeleteRequestFail()
        {
            string privacyDataType = this.policy.DataTypes.Ids.DemographicInformation.Value;
            DeleteRequest deleteRequestMsa = new DeleteRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.Delete,
                PrivacyDataType = privacyDataType
            };

            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(deleteRequestMsa);

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task PostCommandsAsyncDeleteRequestSuccess()
        {
            string privacyDataType = this.policy.DataTypes.Ids.DemographicInformation.Value;
            DeleteRequest deleteRequestMsa = new DeleteRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.Delete,
                PrivacyDataType = privacyDataType
            };

            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(deleteRequestMsa);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        public async Task PostCommandsAsyncExportRequestFail()
        {
            var privacyDataTypes = new[]
            {
                this.policy.DataTypes.Ids.DemographicInformation.Value,
                this.policy.DataTypes.Ids.Account.Value,
                this.policy.DataTypes.Ids.CloudServiceProvider.Value,
                this.policy.DataTypes.Ids.CommuteAndTravel.Value
            };
            ExportRequest exportRequest = new ExportRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.Export,
                PrivacyDataTypes = privacyDataTypes
            };
            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(exportRequest);

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task PostCommandsAsyncExportRequestSuccess()
        {
            var privacyDataTypes = new[]
            {
                this.policy.DataTypes.Ids.DemographicInformation.Value,
                this.policy.DataTypes.Ids.Account.Value,
                this.policy.DataTypes.Ids.CloudServiceProvider.Value,
                this.policy.DataTypes.Ids.CommuteAndTravel.Value
            };
            ExportRequest exportRequest = new ExportRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.Export,
                PrivacyDataTypes = privacyDataTypes
            };
            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(exportRequest);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        public async Task PostCommandsAsyncUnknownRequestfail()
        {
            PrivacyRequest privacyRequest = new PrivacyRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.None
            };

            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(privacyRequest);

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task PostCommandsAsyncUnknownRequestSuccess()
        {
            PrivacyRequest privacyRequest = new PrivacyRequest
            {
                AuthorizationId = "p:123456789",
                CloudInstance = "Public",
                CorrelationVector = "cv",
                RequestType = RequestType.None
            };
            List<PrivacyRequest> iPrivacyRequests = this.GenerateRequestPerSubjectType(privacyRequest);

            AdapterResponse actualResult = await this.pcfAdapter.PostCommandsAsync(iPrivacyRequests).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task QueryCommandStatusAsyncExpectedException()
        {
            IPrivacySubject subject = new DeviceSubject();
            string requester = null;
            List<RequestType> requestTypes = null;
            DateTimeOffset oldestCommand = DateTimeOffset.UtcNow;

            var commandStatusResponses = new List<CommandStatusResponse>();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(commandStatusResponses));
            await this.pcfAdapter.QueryCommandStatusAsync(subject, requester, requestTypes, oldestCommand).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task QueryCommandStatusAsyncFail()
        {
            IPrivacySubject subject = new AadSubject();
            List<RequestType> requestTypes = new List<RequestType> { RequestType.Export };
            DateTimeOffset oldestCommand = DateTimeOffset.UtcNow;
            string requester = null;

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            AdapterResponse<IList<CommandStatusResponse>> actualResult = await this.pcfAdapter.QueryCommandStatusAsync(subject, requester, requestTypes, oldestCommand).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task QueryCommandStatusAsyncSuccess()
        {
            IPrivacySubject aadsubject = new AadSubject { ObjectId = Guid.NewGuid() };
            string requester = "dontcare";
            var requestTypes = new List<RequestType> { RequestType.AccountClose, RequestType.Delete, RequestType.Export, RequestType.None };
            DateTimeOffset oldestCommand = DateTimeOffset.UtcNow;

            var commandStatusResponses = new List<CommandStatusResponse>();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(commandStatusResponses));
            AdapterResponse<IList<CommandStatusResponse>> aadQueryCommandResponse = 
                await this.pcfAdapter.QueryCommandStatusAsync(aadsubject, requester, requestTypes, oldestCommand)
                    .ConfigureAwait(false);

            Assert.IsTrue(aadQueryCommandResponse.IsSuccess);
            Assert.IsNotNull(aadQueryCommandResponse.Result);
            Assert.IsNull(aadQueryCommandResponse.Error);

            IPrivacySubject msaSubject = new MsaSubject { Puid = long.MaxValue };
            AdapterResponse<IList<CommandStatusResponse>> msaQueryCommandResponse = 
                await this.pcfAdapter.QueryCommandStatusAsync(msaSubject, requester, requestTypes, oldestCommand)
                    .ConfigureAwait(false);
            Assert.IsTrue(msaQueryCommandResponse.IsSuccess);
            Assert.IsNotNull(msaQueryCommandResponse.Result);
            Assert.IsNull(msaQueryCommandResponse.Error);
        }

        [TestMethod]
        public async Task QueryCommandByCommandIdFail()
        {
            const string Msg = "errormessage";
            const string AssetGroupId = "ASSETGROUPID";
            const string CommandId = "CMDID";
            const string AgentId = "AGENTID";

            AdapterResponse<QueryCommandByIdResult> result;

            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent(Msg);
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(this.response);

            result = await this.pcfAdapter
                .QueryCommandByCommandIdAsync(AgentId, AssetGroupId, CommandId, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, result.Error.Code);
            Assert.AreEqual(Msg, result.Error.Message);
        }

        [TestMethod]
        public async Task QueryCommandByCommandIdSuccess()
        {
            const string AssetGroupId = "ASSETGROUPID";
            const string CommandId = "CMDID";
            const string AgentId = "AGENTID";

            QueryCommandByIdResult expected = new QueryCommandByIdResult
            {
                ResponseCode = ResponseCode.OK,
                Command = new JObject(new JProperty("CommandId", CommandId))
            };

            AdapterResponse<QueryCommandByIdResult> result;

            this.response.Content = new StringContent(JsonConvert.SerializeObject(expected));

            result = await this.pcfAdapter
                .QueryCommandByCommandIdAsync(AgentId, AssetGroupId, CommandId, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expected.ResponseCode, result.Result.ResponseCode);

            Assert.AreEqual(
                expected.Command.SelectToken("CommandId").Value<string>(),
                result.Result.Command.SelectToken("CommandId").Value<string>());
        }

        private AgentQueueStatisticsResponse GenerateAgentQueueStatisticsResponse(
            int pendingCommandCount = 1,
            int unleasedCommandCount = 2)
        {
            return new AgentQueueStatisticsResponse
            {
                AssetGroupQueueStatistics = new List<AssetGroupQueueStatistics>
                {
                    new AssetGroupQueueStatistics
                    {
                        AgentId = Guid.NewGuid(),
                        AssetGroupId = Guid.NewGuid(),
                        PendingCommandCount = pendingCommandCount,
                        UnleasedCommandCount = unleasedCommandCount,
                        SubjectType = nameof(AadSubject)
                    }
                }
            };
        }

        private CommandStatusResponse GenerateCommandStatusResponse()
        {
            return new CommandStatusResponse
            {
                Context = "doesntcare",
                CommandId = Guid.NewGuid(),
                TotalCommandCount = 2,
                CommandType = "Delete",
                Subject = new AadSubject(),
                SubjectType = nameof(AadSubject),
                Requester = "dashboard",
                CreatedTime = DateTimeOffset.UtcNow,
                CompletedTime = DateTimeOffset.UtcNow.AddMinutes(4),
                CompletionSuccessRate = 4.3,
                FinalExportDestinationUri = new Uri("https://doesnotmatter.com"),
                IngestionDataSetVersion = long.MaxValue,
                IsGloballyComplete = false,
                IngestionAssemblyVersion = "v1.3",
                DataTypes = new List<string>(),
                IsSyntheticCommand = true,
                PredicateType = "dontknow",
                AssetGroupStatuses = new List<AssetGroupCommandStatus>()
            };
        }

        private List<PrivacyRequest> GenerateRequestPerSubjectType<T>(T privacyRequestTemplate)
            where T : PrivacyRequest
        {
            List<PrivacyRequest> iPrivacyRequests = new List<PrivacyRequest>();

            for (int i = 0; i < 8; ++i)
            {
                PrivacyRequest clonedPrivacyRequest = privacyRequestTemplate.ShallowCopyWithNewId();
                switch (i)
                {
                    case 0:
                        clonedPrivacyRequest.Subject = new MsaSubject { Xuid = "0123456789" };
                        break;
                    case 1:
                        clonedPrivacyRequest.Subject = new AadSubject();
                        break;
                    case 2:
                        clonedPrivacyRequest.Subject = new DeviceSubject();
                        break;
                    case 3:
                        clonedPrivacyRequest.Subject = new DeviceSubject { GlobalDeviceId = long.MaxValue };
                        break;
                    case 4:
                        clonedPrivacyRequest.Subject = new DeviceSubject { XboxConsoleId = long.MaxValue };
                        break;
                    case 5:
                        clonedPrivacyRequest.Subject = new DemographicSubject();
                        break;
                    case 6:
                        clonedPrivacyRequest.Subject = new MicrosoftEmployee();
                        break;
                    case 7:
                        clonedPrivacyRequest.Subject = null;
                        break;
                }

                iPrivacyRequests.Add(clonedPrivacyRequest);
            }

            return iPrivacyRequests;
        }
    }
}
