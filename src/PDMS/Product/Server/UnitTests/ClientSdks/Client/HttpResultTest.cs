namespace Microsoft.PrivacyServices.DataManagement.Client.UnitTest
{
    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class HttpResultTest
    {
        [Theory(DisplayName = "When HttpResult.Convert is called, then store converted value."), AutoMoqData]
        public void VerifyConvert(HttpResult<string> result)
        {
            var newResult = result.Convert(__ => 1, 2);
            Assert.Equal(1, newResult.Response);
        }
    }
}