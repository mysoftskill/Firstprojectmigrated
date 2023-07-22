namespace PCF.FunctionalTests
{
    using System;
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
    public class AgentQueueFlushFunctionalTests : FunctionalTestBase
    {
        public AgentQueueFlushFunctionalTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task CanEnqueueAgentFlushRequest()
        {
            string agentId = "14b1e8de-19ad-4132-9344-abfe28f37d04";
            string flushDate = "20180421";
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/flushqueue/{agentId}/{flushDate}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CallAgentFlushQueueWithInvalidAgentReturnsBadRequest()
        {
            string agentId = "invalid";
            string flushDate = "20180421";
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/flushqueue/{agentId}/{flushDate}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CallAgentFlushQueueWithNonexistantAgentReturnsBadRequest()
        {
            string agentId = "D054EE68-6494-4ACD-8A09-D75BFEE58247";
            string flushDate = "20180421";
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/flushqueue/{agentId}/{flushDate}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CallAgentFlushQueueWithInvalidDateReturnsBadRequest()
        {
            string agentId = "14b1e8de-19ad-4132-9344-abfe28f37d04";
            string flushDate = "invalid";
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/flushqueue/{agentId}/{flushDate}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Cannot enqueue agent flush request using HttpMethod Head.
        /// </summary>
        [Fact]
        public async Task CannotEnqueueAgentFlushRequestUsingHttpMethodHead()
        {
            string agentId = "14b1e8de-19ad-4132-9344-abfe28f37d04";
            string flushDate = "20180421";
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Head, $"https://{TestSettings.ApiHostName}/testhooks/flushqueue/{agentId}/{flushDate}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

#endif
}