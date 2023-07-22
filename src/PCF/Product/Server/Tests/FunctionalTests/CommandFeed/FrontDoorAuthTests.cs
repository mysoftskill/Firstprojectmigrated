namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    using Xunit;
    using Xunit.Abstractions;
    
    [Trait("Category", "FCT")]
    public class FrontDoorAuthTests
    {
        private static readonly Guid AgentId = Guid.Parse("9A7F4284-2F84-4733-AC27-6F7A99725D1F");

        private readonly ITestOutputHelper outputHelper;

        public FrontDoorAuthTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        /// <summary>
        /// Ensures that GetCommands fails if the MSA site id is invalid.
        /// </summary>
        [Fact]
        public async Task GetCommandsFailsWithInvalidSiteId()
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                AgentId,
                TestSettings.TestInvalidSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                TestSettings.TestEndpointConfig);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetCommandsAsync(CancellationToken.None));
            Assert.Contains("invalid_client", ex.Message);
        }

        /// <summary>
        /// Ensures that GetCommands returns an exception with an single tenant AME client id called in MS Tenant.
        /// </summary>
        [Fact]
        public async Task GetCommandsFailsWithInvalidClientId()
        {
            var ex = await Assert.ThrowsAsync<MsalServiceException>(async () => await this.GetCommandsAsync(TestSettings.TestInvalidClientIdAME, TestSettings.TestEndpointConfig));
            Assert.Contains("not found in the directory", ex.Message);
        }

        /// <summary>
        /// Ensures that GetCommands returns an exception with an incorrectly configured AAD client id, AME Tenant.
        /// </summary>
        [Fact]
        public async Task GetCommandsFailsWithInvalidClientIdAMEAuthBadCertificate()
        {
            var ex = await Assert.ThrowsAsync<MsalServiceException>(async () => await this.GetCommandsAsync(TestSettings.TestInvalidClientIdAME, TestSettings.TestEndpointConfigAME)); 
            Assert.Contains("A configuration issue is preventing authentication", ex.Message);
        }

        private async Task<List<IPrivacyCommand>> GetCommandsAsync(string clientId, CommandFeedEndpointConfiguration config)
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                AgentId,
                clientId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                config,
                sendX5c: true);

            return await client.GetCommandsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Ensures that QueryCommand works on an empty queue
        /// </summary>
        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "CustomerContent", SubjectType = typeof(MsaSubject))]
        public async Task QueryCommandFailWithInvalidSiteId(DeleteCommand deleteCommand)
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();
        
            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                AgentId,
                TestSettings.TestSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(), 
                TestSettings.TestEndpointConfig);

            // get a valid lease
            string leaseReceipt = deleteCommand.LeaseReceipt;
            await Assert.ThrowsAsync<MsalServiceException>(async () => await this.GetCommandsAsync(leaseReceipt, TestSettings.TestEndpointConfigAME));
        }

        /// <summary>
        /// Ensures that the Commands API fails if agent id is not registered
        /// </summary>
        [Fact]
        public async Task GetCommandsApiFailsWithInvalidAgentId()
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            var logger = new TestCommandFeedLogger(this.outputHelper);
            var factory = new InsecureHttpClientFactory();
            var httpClient = factory.CreateHttpClient(clientCertificate);
            var authClient = new MicrosoftAccountAuthClient(TestSettings.TestSiteId, logger, httpClient, TestSettings.TestEndpointConfig);

            var agentId = Guid.NewGuid().ToString();
            string uri = $"https://{TestSettings.ApiHostName}:443/pcf/v1/{agentId}/commands";

            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            await this.AddCommonHeadersAsync(authClient, getRequest).ConfigureAwait(false);

            var response = await httpClient.SendAsync(getRequest, CancellationToken.None).ConfigureAwait(false);
            logger.HttpResponseReceived(getRequest, response);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Ensures that the Commands API fails if agent id is not a guid
        /// </summary>
        [Fact]
        public async Task GetCommandsApiFailsWithAgentIdNotAGuid()
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            var logger = new TestCommandFeedLogger(this.outputHelper);
            var factory = new InsecureHttpClientFactory();
            var httpClient = factory.CreateHttpClient(clientCertificate);
            var authClient = new MicrosoftAccountAuthClient(TestSettings.TestSiteId, logger, httpClient, TestSettings.TestEndpointConfig);

            var agentId = "not.a.guid";
            string uri = $"https://{TestSettings.ApiHostName}:443/pcf/v1/{agentId}/commands";

            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            await this.AddCommonHeadersAsync(authClient, getRequest).ConfigureAwait(false);

            var response = await httpClient.SendAsync(getRequest, CancellationToken.None).ConfigureAwait(false);
            logger.HttpResponseReceived(getRequest, response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task AddCommonHeadersAsync(IAuthClient authClient, HttpRequestMessage request)
        {
            var token = await authClient.GetAccessTokenAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(authClient.Scheme, token);

            request.Headers.Add("x-client-version", $"pcfsdk;1.0.0.0");
            request.Headers.Add("x-supported-commands", "AccountClose,Delete,Export");
        }
    }
}
