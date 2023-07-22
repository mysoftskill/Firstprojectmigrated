namespace PCF.FunctionalTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "FCT")]
    public class BasicFrontDoorTests
    {
        private readonly ITestOutputHelper outputHelper;

        public BasicFrontDoorTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        /// <summary>
        /// Ensures that the probe page works.
        /// </summary>
        [Fact]
        public async Task Probe()
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                // Insecure. We want to ensure that there is a certificate, but we don't require it to be valid for our tests.
                handler.ServerCertificateValidationCallback = (a, b, c, d) =>
                {
                    Assert.NotNull(b);
                    return true;
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    string uri = $"https://{TestSettings.ApiHostName}:443/keepalive";
                    var response = await client.GetAsync(uri);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    // verify "x-content-type-options: nosniff" header exists
                    Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var sniff));
                    Assert.Equal("nosniff", sniff.First());
                }
            }

            X509Certificate2 cert = await TestSettings.GetStsCertificateAsync();
            Assert.NotNull(cert);
            this.outputHelper.WriteLine(cert.ToString());
        }

        /// <summary>
        /// Ensures that the OpenApi document page works.
        /// </summary>
        [Fact]
        public async Task OpenApiDocument()
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                // Insecure. We want to ensure that there is a certificate, but we don't require it to be valid for our tests.
                handler.ServerCertificateValidationCallback = (a, b, c, d) =>
                {
                    Assert.NotNull(b);
                    return true;
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    string uri = $"https://{TestSettings.ApiHostName}:443/v1/openapi";
                    var response = await client.GetAsync(uri);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Assert.NotNull(content);

                    // A few simple checks that the content is what we expect
                    Assert.Contains("openapi", content);
                    Assert.Contains("Pcf.Frontdoor", content);
                }
            }

            X509Certificate2 cert = await TestSettings.GetStsCertificateAsync();
            Assert.NotNull(cert);
            this.outputHelper.WriteLine(cert.ToString());
        }

        /// <summary>
        /// Ensures that GetCommands works on an empty queue
        /// </summary>
        [Fact]
        public async Task GetCommandsLongpoll()
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                Guid.Parse("9A7F4284-2F84-4733-AC27-6F7A99725D1F"),
                TestSettings.TestSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                TestSettings.TestEndpointConfig);

            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            var commands = await client.GetCommandsAsync(CancellationToken.None);

            Assert.NotNull(commands);

            await Task.WhenAll(commands.Select(x => x.CheckpointAsync(CommandStatus.Complete, 0)));
        }

        /// <summary>
        /// Ensures that GetCommands works on an empty queue, with AAD Auth.
        /// </summary>
        [Fact]
        public async Task GetCommandsLongpollWithAadAuth()
        {
            await this.GetCommandsLongPollAsync(TestSettings.TestClientId, TestSettings.TestEndpointConfig);
        }

        /// <summary>
        /// Ensures that GetCommands works on an empty queue, with AAD Auth.
        /// </summary>
        [Fact]
        public async Task GetCommandsLongpollWithAadAMEAuth()
        {
            await this.GetCommandsLongPollAsync(TestSettings.TestClientIdAME, TestSettings.TestEndpointConfigAME);
        }

        /// <summary>
        /// Ensures that GetCommands works on an empty queue, with AAD Auth.
        /// </summary>
        [Fact]
        public async Task GetCommandsLongpollWithAadMultitenantAMEAuth()
        {
            await this.GetCommandsLongPollAsync(TestSettings.TestClientIdAME, TestSettings.TestEndpointConfigAMEMultitenant);
        }

        private async Task GetCommandsLongPollAsync(string clientId, CommandFeedEndpointConfiguration config)
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                Guid.Parse("9A7F4284-2F84-4733-AC27-6F7A99725D1F"),
                clientId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                config,
                sendX5c: true);

            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            var commands = await client.GetCommandsAsync(CancellationToken.None);

            Assert.NotNull(commands);

            // Try to complete anything that has come in, but we're not testing checkpoint here.
            try
            {
                await Task.WhenAll(commands.Select(x => x.CheckpointAsync(CommandStatus.Complete, 0)));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Ensures that QueryCommand fails for bad lease receipt
        /// </summary>
        [Fact]
        public async Task QueryCommandFailsWithMalformedLeaseReceipt()
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();
        
            // Use a special agent for this test that we never write any commands to. This allows the test to more reliably pass
            // since there is not any pollution.
            var client = new CommandFeedClient(
                Guid.Parse("9A7F4284-2F84-4733-AC27-6F7A99725D1F"),
                TestSettings.TestSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(), 
                TestSettings.TestEndpointConfig);

            // Guid.NewGuid() is *not* a well-formed lease receipt.
            var leaseReceipt = Guid.NewGuid().ToString();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.QueryCommandAsync(leaseReceipt, CancellationToken.None));
            Assert.Contains("MalformedLeaseReceipt", ex.Message);
        }
    }
}
