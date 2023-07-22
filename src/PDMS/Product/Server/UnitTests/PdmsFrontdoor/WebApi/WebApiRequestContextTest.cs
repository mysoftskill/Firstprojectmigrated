namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System.Net.Http;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class WebApiRequestContextTest
    {
        [Theory(DisplayName = "When Set is called, then store the data on requestMessage.Properties."), AutoMoqData]
        public void VerifyGet(string key, string data, HttpRequestMessage message, WebApiRequestContextFactory factory)
        {
            var requestContext = factory.Create(message);
            message.Properties[key] = data;

            Assert.Equal(data, requestContext.Get<string>(key));
        }

        [Theory(DisplayName = "When Set is called, then retrieve the data from requestMessage.Properties."), AutoMoqData]
        public void VerifySet(string key, string data, HttpRequestMessage message, WebApiRequestContextFactory factory)
        {
            var requestContext = factory.Create(message);
            requestContext.Set(key, data);
            Assert.Equal(data, message.Properties[key] as string);
        }
    }
}