#if INCLUDE_TEST_HOOKS
namespace PCF.FunctionalTests.NonWindowsDeviceDeleteWorker
{
    using Microsoft.Azure.ComplianceServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "FCT")]
    public class NonWindowsDeviceDeleteWorkerTests : EndToEndTestBase
    {
        // Test AgentId 2 that has NonWindowsDevice subject tag in TestDataMap
        private static readonly Guid AgentId = Guid.Parse("6E5A7DCE-6FF8-4BFC-A224-3235D702DABA");

        /// <summary>
        /// NonWindowsDeviceDeleteTests.
        /// </summary>
        public NonWindowsDeviceDeleteWorkerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        // Do not run testhooks tests in PROD
        [Fact(DisplayName = "Can run NonWindowsDeviceWorker.")]
        public async Task CanRunNonWindowsDeviceWorkerAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/nonwindowsdevice/canruneventhubprocessor/");

            var response = await TestSettings.SendWithS2SAync(request, this.outputHelper);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(DisplayName = "E2E Test: send raw event to eventhub, process it and receive corresponding PCF commands back.")]
        public async Task CanProcessEventHubDeleteRequest()
        {
            Guid macOsDeviceId = Guid.NewGuid();
            string deviceId = $"u:{macOsDeviceId.ToString().ToUpperInvariant()}";
            string userId = "CoolUser";
            string cV = "cvSamplexyz.01";
            DateTimeOffset dateTime = DateTimeOffset.UtcNow;
            Guid requestGuid = Guid.NewGuid();

            string json = NonWindowsDeviceTestHelpers.CreateNonWindowsDeviceEvent(
                deviceId: deviceId,
                userId: userId,
                cV: cV,
                time: dateTime);


            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/nonwindowsdevice/senddeleterequest/")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Send delete request to EventHub
            var response = await TestSettings.SendWithS2SAync(request, this.outputHelper);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Get NonWindowsDevice Delete Commands back
            CommandFeedClient client = await this.CreateCommandFeedClientAsync();
            var commands = await client
                .ReceiveNonWindowsDeleteCommandsByDeviceIdAsync(deviceId);

            Assert.True(commands.Any());

            foreach (var command in commands)
            {
                await command.CheckpointAsync(CommandStatus.Complete, 0);
            }
        }

        private async Task<CommandFeedClient> CreateCommandFeedClientAsync()
        {
            X509Certificate2 clientCertificate = await TestSettings.GetStsCertificateAsync();

            return new CommandFeedClient(
                AgentId,
                TestSettings.TestSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                TestSettings.TestEndpointConfig);
        }
    }
}
#endif
