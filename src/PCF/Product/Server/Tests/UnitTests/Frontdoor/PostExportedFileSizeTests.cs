namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Newtonsoft.Json;
    using Ploeh.AutoFixture;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class FrontdoorPostExportedFileSizeTests : INeedDataBuilders
    {
        // Moq doesn't like out and ref parameters until 4.8, so we have to make our own mock object
        private class TestDataAgentInfo : IDataAgentInfo
        {
            public TestDataAgentInfo(bool tryGetResult)
            {
                this.TryGetResult = tryGetResult;
            }

            public AgentId AgentId => null;

            public IEnumerable<IAssetGroupInfo> AssetGroupInfos => null;

            public bool IsOnline => false;

            public bool TryGetResult { get; set; }

            public bool TryGetAssetGroupInfo(AssetGroupId assetGroupId, out IAssetGroupInfo assetGroupInfo) 
            {
                assetGroupInfo = null;
                return this.TryGetResult;
            }

            public bool MatchesMsaSiteId(long msaSiteId) => false;

            public bool MatchesAadAppId(Guid aadAppId) => false;

            public Task MarkAsOnlineAsync() => null;

            public bool IsOptedIntoAadSubject2() => false;

            public bool IsV2Agent() => false;
        }

        [Fact]
        public async Task AgentNotAuthorized()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceiptBuilder leaseReceipt = this.ALeaseReceipt(agentId);

            Mock<IAuthorizer> authorizer = this.AMockOf<IAuthorizer>();
            Mock<IDataAgentMap> map = this.AMockOf<IDataAgentMap>();
            Mock<ILogger> log = this.AMockOf<ILogger>();

            (HttpRequestMessage reqMsg, _) = CreateRequest(leaseReceipt);

            authorizer
                .Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                .ThrowsAsync(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            PostExportedFileSizeActionResult queryCommandAction = 
                new PostExportedFileSizeActionResult(agentId, reqMsg, map.Object, authorizer.Object, log.Object);

            HttpResponseMessage response = await queryCommandAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LeaseReceiptAgentIdMismatch()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt();

            Mock<IAuthorizer> authorizer = this.AMockOf<IAuthorizer>();
            Mock<IDataAgentMap> map = this.AMockOf<IDataAgentMap>();
            Mock<ILogger> log = this.AMockOf<ILogger>();

            (HttpRequestMessage reqMsg, _) = CreateRequest(leaseReceipt);

            PostExportedFileSizeActionResult queryCommandAction =
                new PostExportedFileSizeActionResult(agentId, reqMsg, map.Object, authorizer.Object, log.Object);

            HttpResponseMessage response = await queryCommandAction.ExecuteAsync(CancellationToken.None);

            await FrontdoorPostExportedFileSizeTests.AssertErrorAsync(
                response, 
                HttpStatusCode.BadRequest, 
                PostExportedFileSizeActionResult.PostExportedFileSizeErrorCode.LeaseReceiptAgentIdMismatch);
        }

        [Fact]
        public async Task LeaseReceiptAssetGroupInvalidMismatch()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId);

            Mock<IAuthorizer> authorizer = this.AMockOf<IAuthorizer>();
            Mock<IDataAgentInfo> info = this.AMockOf<IDataAgentInfo>();
            Mock<IDataAgentMap> map = this.AMockOf<IDataAgentMap>();
            Mock<ILogger> log = this.AMockOf<ILogger>();

            (HttpRequestMessage reqMsg, _) = CreateRequest(leaseReceipt);

            map.Setup(o => o[It.IsAny<AgentId>()]).Returns(new TestDataAgentInfo(false));

            PostExportedFileSizeActionResult queryCommandAction =
                new PostExportedFileSizeActionResult(agentId, reqMsg, map.Object, authorizer.Object, log.Object);

            HttpResponseMessage response = await queryCommandAction.ExecuteAsync(CancellationToken.None);

            await FrontdoorPostExportedFileSizeTests.AssertErrorAsync(
                response,
                HttpStatusCode.BadRequest,
                PostExportedFileSizeActionResult.PostExportedFileSizeErrorCode.LeaseReceiptAssetGroupIdInvalid);
        }

        [Fact]
        public async Task CanSubmitResultToLog()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId);

            Mock<IAuthorizer> authorizer = this.AMockOf<IAuthorizer>();
            Mock<IDataAgentMap> map = this.AMockOf<IDataAgentMap>();
            Mock<ILogger> log = this.AMockOf<ILogger>();

            (HttpRequestMessage reqMsg, PostExportedFileSizeRequest reqObj) = CreateRequest(leaseReceipt);

            map.Setup(o => o[It.IsAny<AgentId>()]).Returns(new TestDataAgentInfo(true));

            PostExportedFileSizeActionResult queryCommandAction =
                new PostExportedFileSizeActionResult(agentId, reqMsg, map.Object, authorizer.Object, log.Object);

            HttpResponseMessage response = await queryCommandAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            log.Verify(
                o => o.LogExportFileSizeEvent(
                    agentId,
                    leaseReceipt.AssetGroupId,
                    leaseReceipt.CommandId,
                    reqObj.FileName,
                    reqObj.OriginalSize,
                    reqObj.CompressedSize,
                    reqObj.IsCompressed,
                    SubjectType.Msa,
                    AgentType.NonCosmos,
                    string.Empty),
                Times.Once);
        }

        private static (HttpRequestMessage, PostExportedFileSizeRequest) CreateRequest(
            LeaseReceipt leaseReceipt,
            long originalSize = 20060415,
            long compressedSize = 10030202,
            bool isCompressed = true,
            string filename = "/path/name.txt")
        {
            PostExportedFileSizeRequest request = new Fixture().Create<PostExportedFileSizeRequest>();

            request.LeaseReceipt = leaseReceipt.Serialize();
            request.OriginalSize = originalSize;
            request.CompressedSize = compressedSize;
            request.IsCompressed = isCompressed;
            request.FileName = filename;

            return (new HttpRequestMessage(HttpMethod.Post, "/command") { Content = new JsonContent(request) }, request);
        }

        private static async Task AssertErrorAsync(
            HttpResponseMessage response,
            HttpStatusCode httpStatus,
            PostExportedFileSizeActionResult.PostExportedFileSizeErrorCode expectedError)
        {
            Assert.Equal(response.StatusCode, httpStatus);

            string content = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<PostExportedFileSizeActionResult.PostExportedFileSizeError>(content);

            Assert.Equal(error.ErrorCode, expectedError);
        }
    }
}
