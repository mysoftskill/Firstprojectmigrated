namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System.Net;
    using System.Threading.Tasks;
    using Identity;
    using Setup;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataAssetTests : TestBase
    {
        [TestMethod]
        public async Task WhenISearchDataGridForADataAssetItSucceedsAsync()
        {
            // Checks that we can find the asset qualifier for the delete signals stream
            // TODO: make this configurable, since we might change the path someday...
            var assetQualifier = AssetQualifier.CreateForCosmosStructuredStream("cosmos15", "PXSCosmos15.Prod", "/local/upload/PROD/DeleteSignal/CookedStream/v2");
            var client = TestSetup.PdmsClientInstance;

            var agsResponse = await client.DataAssets.FindByQualifierAsync(assetQualifier, TestSetup.RequestContext)
                .ConfigureAwait(false);

            Assert.AreEqual(agsResponse.HttpStatusCode, HttpStatusCode.OK);

            var results = agsResponse.Response;
            Assert.IsTrue(results.Total > 0, "No results found.");
        }
    }
}
