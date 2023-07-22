namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System.Net.Http;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class OperationMetadataTest
    {
        [Theory(DisplayName = "When FillForWebApi is called with only a request, then verify fields."), AutoMoqData]
        public void VerifyFillForWebApiRequestOnly(HttpRequestMessage request)
        {
            var operationData = new OperationMetadata();
            operationData.FillForWebApi(request);

            Assert.Equal(string.Empty, operationData.CallerIpAddress);
            Assert.Equal(request.RequestUri.Scheme, operationData.Protocol);
            Assert.Equal(request.Method.Method, operationData.RequestMethod);
            Assert.Equal(-1, operationData.RequestSizeBytes);
            Assert.Equal(request.RequestUri.AbsoluteUri, operationData.TargetUri);
        }

        [Theory(DisplayName = "When FillForWebApi is called with a request and response, then verify fields."), AutoMoqData]
        public void VerifyFillForWebApiRequestResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            var operationData = new OperationMetadata();
            operationData.FillForWebApi(request, response);

            Assert.Equal(string.Empty, operationData.CallerIpAddress);
            Assert.Equal(request.RequestUri.Scheme, operationData.Protocol);
            Assert.Equal(request.Method.Method, operationData.RequestMethod);
            Assert.Equal(-1, operationData.RequestSizeBytes);
            Assert.Equal(request.RequestUri.AbsoluteUri, operationData.TargetUri);
            Assert.Equal((int)response.StatusCode, operationData.ProtocolStatusCode);
            Assert.Equal(string.Empty, operationData.ResponseContentType);
        }
    }
}
