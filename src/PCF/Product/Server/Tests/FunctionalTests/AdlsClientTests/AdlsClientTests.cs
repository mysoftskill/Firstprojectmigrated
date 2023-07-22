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
    public class AdlsClientTests: FunctionalTestBase
    {
        private readonly ITestOutputHelper outputHelper;

        public AdlsClientTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        /// <summary>
        /// Ensures that we can read PDMS data into docdb.
        /// </summary>
        [Fact(Skip = "Conflicts with BackgroundTaskTests.RefreshPdmsData")]
        public async Task RefreshPdmsData()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/refreshpdmsdata"),
                this.outputHelper,
                TimeSpan.FromMinutes(5));
            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        /// <summary>
        /// Validate Get Latest Stream Exists.
        /// </summary>
        [Fact]
        public async Task VerifyCanReadCosmosStreamTest()
        {
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/pdms/canreadcosmosstream"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Validate that VariantInfo Stream can be read.
        /// </summary>
        [Fact]
        public async Task VerifyCanReadVariantInfoCosmosStreamTest()
        {
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/pdms/canreadvariantinfofromcosmos"), this.OutputHelper);

            this.Log(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Cannot read Cosmos stream using HttpMethod Head.
        /// </summary>
        [Fact]
        public async Task CannotReadCosmosStreamUsingHttpMethodHeadTest()
        {
            var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Head, $"https://{TestSettings.ApiHostName}/testhooks/pdms/canreadcosmosstream"), this.OutputHelper);
            
            this.Log(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

#endif
}
