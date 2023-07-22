namespace Microsoft.PrivacyServices.DataManagement.Client.UnitTest
{
    using System;

    using Xunit;

    public class OperationNameProviderTest
    {
        [Theory(DisplayName = "When a request is made, then map to friendly name.")]
        [InlineData("/probe", "Probe")]
        [InlineData("/api/v2/dataOwners('9473986C-46EA-4948-B83D-752B5723B3BA')", "V2.DataOwners.Read")]
        public void VerifyAllDataProvidersLoaded(string pathAndQuery, string expectedOperationName)
        {
            Assert.Equal(expectedOperationName, OperationNameProvider.GetFromPathAndQuery("GET", pathAndQuery));
        }

        [Fact(DisplayName = "When a request is made for an unknown api, then throw an exception.")]
        public void VerifyUnknownThrowsException()
        {
            Assert.Throws<ArgumentException>(() => OperationNameProvider.GetFromPathAndQuery("GET", "/unknown"));
        }
    }
}