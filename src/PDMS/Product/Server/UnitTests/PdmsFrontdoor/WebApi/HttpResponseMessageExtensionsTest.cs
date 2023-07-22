namespace Microsoft.PrivacyServices.WebApi.UnitTest
{
    using System.Net.Http;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class HttpResponseMessageExtensionsTest
    {
        [Theory(DisplayName = "When content length is available and GetRequestSizeBytes is called, then return the value."), AutoMoqData]
        public void VerifyGetContentType(string content)
        {
            var message = new HttpResponseMessage();
            message.Content = new StringContent(content);

            Assert.Equal("text/plain; charset=utf-8", message.GetContentType());
        }

        [Fact(DisplayName = "When content is not available and GetContentType is called, then return ''.")]
        public void VerifyGetContentTypeNotAvailable()
        {
            var message = new HttpResponseMessage();
            Assert.Equal(string.Empty, message.GetContentType());
        }
    }
}
