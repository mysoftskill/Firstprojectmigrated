namespace PCF.FunctionalTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Xunit.Abstractions;

    // These tests depend on test-only hooks that are conditionally compiled.
#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// [Namespace].[ClassName].[MethodName]
    /// </summary>
    [Trait("Category", "FCT")]
    public class PdmsFunctionalTests : FunctionalTestBase
    {
        public PdmsFunctionalTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Theory] 
        [InlineAutoData("14b1e8de-19ad-4132-9344-abfe28f37d04", "dbc33a6b-4abe-4d0e-8104-67294992e072")]
        [InlineAutoData("14b1e8de-19ad-4132-9344-abfe28f37d04", "0ac13e25-a1bc-4872-9b60-1dcf24bf3203")]
        [InlineAutoData("9c14b08f-f064-48ba-a221-be13f015ccba", "18bdf572-9b49-4c6b-b5a0-3b5681d80ad8")]
        [InlineAutoData("9c14b08f-f064-48ba-a221-be13f015ccba", "ffbce331-f097-47fe-ae4c-e461cd870933")]
        [InlineAutoData("9a7f4284-2f84-4733-ac27-6f7a99725d1f", "8d00c2b5-56b0-434b-827e-583c5097677d")]
        [InlineAutoData("f3d89dc9-428e-4823-a64c-a243b459de53", "7baa875b-2d94-4008-9d0f-ccbffcfad856")]
        [InlineAutoData("144c01cb-f725-406d-bb60-ed8a3702f290", "59171531-2831-41f0-a749-6c22d41bc04c")]
        public async Task CanCreatePdmsTestDataAgentMap(string aid, string agid)
        {
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/pdms/agentmap/{aid}/{agid}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Cannot create PDMS test data agent map using HttpMethod Head.
        /// </summary>
        [Theory]
        [InlineAutoData("14b1e8de-19ad-4132-9344-abfe28f37d04", "dbc33a6b-4abe-4d0e-8104-67294992e072")]
        public async Task CannotCreatePdmsTestDataAgentMapUsingHttpMethodHead(string aid, string agid)
        {

            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Head, $"https://{TestSettings.ApiHostName}/testhooks/pdms/agentmap/{aid}/{agid}"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

#endif
}