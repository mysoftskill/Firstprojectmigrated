namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup
{
    using System.Net.Http;
    using Microsoft.PrivacyServices.DataManagement.Client;

    public sealed class HttpProxyClient : BaseHttpServiceProxy
    {
        public HttpProxyClient(HttpClient httpClient) : base(httpClient)
        {
        }
    }
}
