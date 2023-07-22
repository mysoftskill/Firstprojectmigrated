namespace PCF.FunctionalTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

#if INCLUDE_TEST_HOOKS

    [Trait("Category", "FCT")]
    public class DatabaseConnectionInfoTests : FunctionalTestBase
    {
        private readonly ITestOutputHelper outputHelper;

        public DatabaseConnectionInfoTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            this.outputHelper = outputHelper;
        }
            /// <summary>
            /// Validate the database connection info from configs.
            /// </summary>
            [Fact]
            public async Task VerifyDatabaseConnectionsFromConfigTest()
            {
                var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/pcf/canconfiguredatabaseconnection"), this.OutputHelper);

                this.Log(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
    }
#endif
 }