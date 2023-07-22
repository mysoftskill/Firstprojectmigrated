namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using Xunit;

    public class OperationNameProviderTest
    {
        [Theory(DisplayName = "When a request is made, then map to friendly name.")]
        [InlineData("/keepalive", "KeepAlive", true)]
        [InlineData("/probe", "Probe", false)]
        [InlineData("/openapi", "OpenApi", false)]
        [InlineData("/unknown", "Unknown.Api", true)]
        public void VerifyAllDataProvidersLoaded(string pathAndQuery, string expectedOperationName, bool expectedIncludeValue)
        {
            var provider = new OperationNameProvider();
            var value = provider.GetFromPathAndQuery("GET", pathAndQuery);
            Assert.Equal(expectedOperationName, value.FriendlyName);
            Assert.Equal(expectedIncludeValue, value.IncludeInTelemetry);
        }
    }
}