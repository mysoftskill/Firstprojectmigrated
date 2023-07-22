namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OpenApiHandlerTests : TestBase
    {
        [TestMethod]
        public async Task WhenICallOpenApiIGetNonZeroResultsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("openapi").ConfigureAwait(false);
            Assert.IsNotNull(content);

            // A few simple checks that the content is what we expect
            Assert.IsTrue(content.Contains("openapi"));
            Assert.IsTrue(content.Contains("PdmsFrontdoor"));
        }
    }
}
