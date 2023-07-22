namespace PCF.FunctionalTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Xunit;
    using Xunit.Abstractions;

    // These tests depend on test-only hooks that are conditionally compiled.
#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// [Namespace].[ClassName].[MethodName]
    /// </summary>
    [Trait("Category", "FCT")]
    public class TelemetryFunctionalTests : FunctionalTestBase
    {
        public TelemetryFunctionalTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        /// <summary>
        /// Verify that lifecycle event could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAddLifecycleEventAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddlifecycleevent"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that baseline record could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAddBaselineAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddbaseline"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify agent azure storage queue depth could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAddAzureStorageQueueDepthAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddazurestoragequeuedepth"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify if baseline task could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAddTaskAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddtask"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that multiply baseline tasks could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAddTasksAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddtasks"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that multiply baseline tasks could be added to telemetry database.
        /// </summary>
        [Fact]
        public async Task CanAppendAgentStatAsync()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canappendagentstat");

            var response = await TestSettings.SendWithS2SAync(httpRequestMessage, this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify CanRunInterpolationQueryAsync.
        /// </summary>
        [Fact]
        public async Task CanRunInterpolationQueryAsync()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canruninterpolationquery");

            var response = await TestSettings.SendWithS2SAync(httpRequestMessage, this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify CanStartQueueDepthBaseline.
        /// </summary>
        [Fact]
        public async Task CanStartQueueDepthBaselineAsync()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canstartqdbaseline");

            var response = await TestSettings.SendWithS2SAync(httpRequestMessage, this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that lifecycle event could not be added to telemetry database using HttpMethod Head.
        /// </summary>
        [Fact]
        public async Task CannotAddLifecycleEventUsingHttpMethodHeadAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                    new HttpRequestMessage(HttpMethod.Head, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddlifecycleevent"),
                    this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Verify baseline task could not be added to telemetry database using HttpMethod Head.
        /// </summary>
        [Fact]
        public async Task CannotAddTaskUsingHttpMethodHeadAsync()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Head, $"https://{TestSettings.ApiHostName}/testhooks/telemetry/canaddtask"),
                this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

#endif
}