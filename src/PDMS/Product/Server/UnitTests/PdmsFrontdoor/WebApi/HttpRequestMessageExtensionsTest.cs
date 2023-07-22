namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.Web;

    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Xunit;

    public class HttpRequestMessageExtensionsTest
    {
        [Theory(DisplayName = "When GetClientIpAddress is called from IIS, then use HttpContext."), AutoMoqData]
        public void GetClientIpAddressFromHttpContext(string ipAddress)
        {
            // Arrange
            var request = new Mock<HttpRequestBase>();
            request.SetupGet(m => m.UserHostAddress).Returns(ipAddress);

            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(m => m.Request).Returns(request.Object);

            var message = new HttpRequestMessage();
            message.Properties["MS_HttpContext"] = httpContext.Object;

            // Act
            var ip = message.GetClientIpAddress();

            // Assert
            Assert.Equal(ipAddress, ip);
        }

        [Theory(DisplayName = "When GetClientIpAddress is called from OWIN, then use RemoteEndpointMessageProperty."), AutoMoqData]
        public void GetClientIpAddressForOwin(string ipAddress, int port)
        {
            // Arrange
            var message = new HttpRequestMessage();
            var messageProperty = new RemoteEndpointMessageProperty(ipAddress, port);
            message.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"] = messageProperty;

            // Act
            var ip = message.GetClientIpAddress();

            // Assert
            Assert.Equal(ipAddress, ip);
        }

        [Theory(DisplayName = "GetClientIpAddress null checks:"),
            InlineData("MS_HttpContext"),
            InlineData("System.ServiceModel.Channels.RemoteEndpointMessageProperty"),
            InlineData("other")]
        public void GetClientIpAddressNullChecks(string propertyName)
        {
            var message = new HttpRequestMessage();
            message.Properties[propertyName] = null;

            Assert.Empty(message.GetClientIpAddress());
        }

        [Theory(DisplayName = "When content length is available and GetRequestSizeBytes is called, then return size."), AutoMoqData]
        public void VerifyGetRequestSizeBytes(string content)
        {
            var message = new HttpRequestMessage();
            message.Content = new StringContent(content);
            
            Assert.Equal(content.Length, message.GetRequestSizeBytes());
        }

        [Fact(DisplayName = "When content is not available and GetRequestSizeBytes is called, then return -1.")]
        public void VerifyGetRequestSizeBytesNotAvailable()
        {
            var message = new HttpRequestMessage();
            Assert.Equal(-1, message.GetRequestSizeBytes());
        }
    }
}
