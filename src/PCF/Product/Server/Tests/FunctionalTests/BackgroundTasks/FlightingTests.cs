namespace PCF.FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Xunit;
    using Xunit.Abstractions;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

#if INCLUDE_TEST_HOOKS

    [Trait("Category", "FCT")]
    public class FlightingTests
    {
        private readonly ITestOutputHelper outputHelper;

        public FlightingTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        /// <summary>
        /// Calls an API that only returns OK if the flight is enabled.
        /// </summary>
        [Fact]
        public async Task FlightingInitializedCorrectly()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/flighting/initializationcheck"),
                this.outputHelper);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

#endif
}
