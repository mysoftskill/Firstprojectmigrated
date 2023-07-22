namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class GetCommandsTests : INeedDataBuilders
    {
        private Mock<IApiTrafficHandler> mockApiTrafficHandler;

        public GetCommandsTests()
        {
            this.mockApiTrafficHandler = new Mock<IApiTrafficHandler>();
            this.mockApiTrafficHandler.Setup(
                m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "GetCommands", It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            this.mockApiTrafficHandler.Setup(
                m => m.GetTooManyRequestsResponse()).Returns(
                new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("Too Many Requests. Retry later with suggested delay in retry header."),
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)) }
                });
        }


        [Fact]
        public async Task AgentNotAuthorized()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();

            var agentId = this.AnAgentId();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                CreateGetCommandsRequest(),
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AgentIsBlocked()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();

            var agentId = this.AnAgentId();

            var request = CreateGetCommandsRequest();

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                request,
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            using (new FlightEnabled(FlightingNames.BlockedAgents))
            {
                var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        [Fact]
        public async Task AgentMustUseVerifier()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();

            var agentId = this.AnAgentId();

            var request = CreateGetCommandsRequest();
            request.Headers.Add("x-client-version", "pcfsdk;1.2.3.4;v:false");

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                request,
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            bool configval = Config.Instance.Frontdoor.AllowSdkWithoutVerifier;

            try
            {
                Config.Instance.Frontdoor.AllowSdkWithoutVerifier = false;
                var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            finally
            {
                Config.Instance.Frontdoor.AllowSdkWithoutVerifier = configval;
            }
        }

        [Fact]
        public async Task RequestedLeaseDuration()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var aadCommandQueue = this.AMockOf<ICommandQueue>();
            aadCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.High);

            TimeSpan? computedLeaseDuration = null;

            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();
            var deleteCommands = Enumerable.Range(1, 1000)
                .Select(x =>
                    this.ADeleteCommand(agentId, assetGroupId)
                        .WithValue(t => t.NextVisibleTime, DateTimeOffset.UtcNow.AddMinutes(5))
                        .Build()).ToList();
            var mockAssetGroupInfo = ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);
            mockAssetGroupInfo.SetupGet(m => m.SupportedSubjectTypes).Returns(new[] { SubjectType.Aad });
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Aad, QueueStorageType.AzureCosmosDb))
                .Returns(aadCommandQueue.Object);

            aadCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandQueuePriority>())).Callback((int pop, TimeSpan? ld, CommandQueuePriority cqp) => computedLeaseDuration = ld)
                .ReturnsAsync(new CommandQueuePopResult(deleteCommands.Take(100).Cast<PrivacyCommand>().ToList(), null));

            var inputRequestedLeaseDuration = new[] { null, "-1", "0", "1", "10", "3600", int.MaxValue.ToString(), "invalid" };
            var expectedLeaseDuration = new int?[] { null, null, null, null, 10, 3600, null, null };
            for (int i = 0; i < inputRequestedLeaseDuration.Length; i++)
            {
                var request = CreateGetCommandsRequest();

                if (inputRequestedLeaseDuration[i] != null)
                {
                    request.Headers.Add("x-lease-duration-seconds", inputRequestedLeaseDuration[i]);
                }

                GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                    agentId,
                    request,
                    commandQueueFactory.Object,
                    dataAgentMap.Object,
                    publisher.Object,
                    authorizer.Object,
                    this.mockApiTrafficHandler.Object);

                var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                if (expectedLeaseDuration[i].HasValue)
                {
                    Assert.Equal(expectedLeaseDuration[i].Value, computedLeaseDuration.Value.TotalSeconds);
                }
                else
                {
                    Assert.Null(computedLeaseDuration);
                }
            }
        }

        [Fact]
        public async Task CommandsImmediatelyReturned()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var msaCommandQueue = this.AMockOf<ICommandQueue>();
            msaCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.High);
            var aadCommandQueue = this.AMockOf<ICommandQueue>();
            aadCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.High);

            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var deleteCommands = Enumerable.Range(1, 1000)
                .Select(x =>
                    this.ADeleteCommand(agentId, assetGroupId)
                    .WithValue(t => t.NextVisibleTime, DateTimeOffset.UtcNow.AddMinutes(5))
                .Build()).ToList();

            var mockAssetGroupInfo = ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);
            mockAssetGroupInfo.SetupGet(m => m.SupportedSubjectTypes).Returns(new[] { SubjectType.Msa, SubjectType.Aad });
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Msa, QueueStorageType.AzureCosmosDb))
                               .Returns(msaCommandQueue.Object);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Aad, QueueStorageType.AzureCosmosDb))
                               .Returns(aadCommandQueue.Object);

            msaCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), null, It.IsAny<CommandQueuePriority>()))
                        .ReturnsAsync(new CommandQueuePopResult(deleteCommands.Take(100).Cast<PrivacyCommand>().ToList(), null));

            aadCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), null, It.IsAny<CommandQueuePriority>()))
                        .ReturnsAsync(new CommandQueuePopResult(null, null));

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                CreateGetCommandsRequest(),
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<GetCommandsResponse>(body);

            Assert.NotNull(parsedResponse.DeleteCommands);
            Assert.True(parsedResponse.DeleteCommands.Count > 0);
        }

        [Fact]
        public async Task LowPriorityCommandsReturned()
        {
            var timer = Stopwatch.StartNew();
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var msaCommandQueue = this.AMockOf<ICommandQueue>();
            msaCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.Low);
            var aadCommandQueue = this.AMockOf<ICommandQueue>();
            aadCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.High);

            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var ageOutCommands = Enumerable.Range(1, 1000)
                .Select(x =>
                    this.AnAgeOutCommand(agentId, assetGroupId)
                    .WithValue(t => t.NextVisibleTime, DateTimeOffset.UtcNow.AddMinutes(5))
                .Build()).ToList();

            var deleteCommands = Enumerable.Range(1, 1)
                .Select(x =>
                    this.ADeleteCommand(agentId, assetGroupId)
                        .WithValue(t => t.NextVisibleTime, DateTimeOffset.UtcNow.AddMinutes(5))
                        .Build()).ToList();

            var mockAssetGroupInfo = ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);
            mockAssetGroupInfo.SetupGet(m => m.SupportedSubjectTypes).Returns(new[] { SubjectType.Msa, SubjectType.Aad });
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());
            mockAssetGroupInfo.Setup(m => m.SupportsLowPriorityQueue).Returns(true);

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Msa, QueueStorageType.AzureQueueStorage))
                               .Returns(msaCommandQueue.Object);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Aad, QueueStorageType.AzureCosmosDb))
                               .Returns(aadCommandQueue.Object);

            msaCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), null, It.IsAny<CommandQueuePriority>()))
                        .ReturnsAsync(new CommandQueuePopResult(ageOutCommands.Take(100).Cast<PrivacyCommand>().ToList(), null));

            aadCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), null, It.IsAny<CommandQueuePriority>()))
                        .ReturnsAsync(new CommandQueuePopResult(deleteCommands.Take(0).Cast<PrivacyCommand>().ToList(), null));

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                CreateGetCommandsRequest(),
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
            timer.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<GetCommandsResponse>(body);

            Assert.NotNull(parsedResponse.AgeOutCommands);
            Assert.True(parsedResponse.AgeOutCommands.Count > 0);
            Assert.True(timer.Elapsed >= GetCommandsActionResult.MaxHighPriorityTimeReducedDefault);
        }

        [Theory]
        [InlineData(false, "1.1.1.1", typeof(AadSubject))]
        [InlineData(false, "1.6.2000.0", typeof(AadSubject))]
        [InlineData(true, "1.1.1.1", typeof(AadSubject))]
        [InlineData(true, "1.6.2000.0", typeof(AadSubject2))]
        public async Task AgentSupportsMultiTenantCollaboration(bool agentSupportsMultiTenantCollaboration, string sdkVersion, Type expectedSubjectType)
        {
            const string V2Verifier = "verifierv2";
            const string V3Verifier = "verifierv3";

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var aadCommandQueue = this.AMockOf<ICommandQueue>();
            aadCommandQueue.SetupGet(c => c.QueuePriority).Returns(CommandQueuePriority.High);

            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var accountCloseCommand = Enumerable.Range(1, 10)
                .Select(x =>
                    this.AnAccountCloseCommand(agentId, assetGroupId)
                    .WithValue(t => t.NextVisibleTime, DateTimeOffset.UtcNow.AddMinutes(5))
                    .With(t => t.Subject, expectedSubjectType == typeof(AadSubject) ?
                        new AadSubject { TenantId = Guid.NewGuid(), ObjectId = Guid.NewGuid() } :
                        new AadSubject2 { TenantId = Guid.NewGuid(), ObjectId = Guid.NewGuid() }
                        )
                    .With(t => t.Verifier, V2Verifier)
                    .With(t => t.VerifierV3, V3Verifier)
                .Build()).ToList();

            var mockAssetGroupInfo = ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId, agentSupportsMultiTenantCollaboration);
            mockAssetGroupInfo.SetupGet(m => m.SupportedSubjectTypes).Returns(new[] { SubjectType.Aad, SubjectType.Aad2 });
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            mockAssetGroupInfo.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            commandQueueFactory.Setup(m => m.CreateQueue(agentId, assetGroupId, SubjectType.Aad2, QueueStorageType.AzureCosmosDb))
                               .Returns(aadCommandQueue.Object);

            aadCommandQueue.Setup(m => m.PopAsync(It.IsAny<int>(), null, It.IsAny<CommandQueuePriority>()))
                        .ReturnsAsync(new CommandQueuePopResult(accountCloseCommand.Cast<PrivacyCommand>().ToList(), null));

            var httpRequestMessage = CreateGetCommandsRequest();
            httpRequestMessage.Headers.Add("x-client-version", $"pcfsdk;{sdkVersion};");

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                httpRequestMessage,
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<GetCommandsResponse>(body);

            Assert.NotNull(parsedResponse.AccountCloseCommands);
            Assert.True(parsedResponse.AccountCloseCommands.Count > 0);
            Assert.IsType(expectedSubjectType, parsedResponse.AccountCloseCommands[0].Subject);

            if (expectedSubjectType == typeof(AadSubject2))
            {
                Assert.Equal(V3Verifier, parsedResponse.AccountCloseCommands[0].Verifier);
            }
            else
            {
                Assert.Equal(V2Verifier, parsedResponse.AccountCloseCommands[0].Verifier);
            }
        }

        [Fact]
        public async Task GetCommandsRequestGetsThrottled()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueueFactory = this.AMockOf<ICommandQueueFactory>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();

            var agentId = this.AnAgentId();

            var request = CreateGetCommandsRequest();

            // throttle traffic for given agent
            this.mockApiTrafficHandler.Setup(
                m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "GetCommands", agentId.ToString(), It.IsAny<string>())).Returns(false);

            GetCommandsActionResult getCommandsAction = new GetCommandsActionResult(
                agentId,
                request,
                commandQueueFactory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object,
                this.mockApiTrafficHandler.Object);

            var response = await getCommandsAction.ExecuteAsync(CancellationToken.None);
            
            Assert.Equal((HttpStatusCode)429, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Too Many Requests. Retry later with suggested delay in retry header.", content);

            Assert.Equal(TimeSpan.FromSeconds(5), response.Headers.RetryAfter.Delta.Value);
        }

        private static HttpRequestMessage CreateGetCommandsRequest()
        {
            return new HttpRequestMessage(HttpMethod.Get, "/getcommands");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private Mock<IAssetGroupInfo> ConfigureDataAgentMap(Mock<IDataAgentMap> dataAgentMap, AgentId agentId, AssetGroupId assetGroupId, bool agentSupportsMultiTenantCollaboration = false)
        {
            Mock<IAssetGroupInfo> mockAssetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(assetGroupId);
            IAssetGroupInfo assetGroupInfo = mockAssetGroupInfo.Object;

            Mock<IDataAgentInfo> dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { mockAssetGroupInfo.Object });
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(It.IsAny<AssetGroupId>(), out assetGroupInfo)).Returns(true);
            dataAgentInfo.Setup(m => m.IsOptedIntoAadSubject2()).Returns(agentSupportsMultiTenantCollaboration);

            IDataAgentInfo agentInfo = dataAgentInfo.Object;

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentMap.Setup(m => m.TryGetAgent(agentId, out agentInfo)).Returns(true);

            return mockAssetGroupInfo;
        }
    }
}
