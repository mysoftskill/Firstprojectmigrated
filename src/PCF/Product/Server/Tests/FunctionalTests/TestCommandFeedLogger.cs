namespace PCF.FunctionalTests
{
    using System;
    using System.Net.Http;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Xunit.Abstractions;

    public class TestCommandFeedLogger : CommandFeedLogger
    {
        private readonly ITestOutputHelper outputHelper;

        public TestCommandFeedLogger(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public HttpResponseMessage LastResponse { get; private set; }

        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            this.LastResponse = response;
            this.outputHelper.WriteLine($"HTTP Response Received! Url = {request.RequestUri}, ResponseCode = {response.StatusCode}, ResponseBody = {response.Content.ReadAsStringAsync().Result}");
        }

        public override void UnhandledException(Exception ex)
        {
            this.outputHelper.WriteLine(ex.ToString());
        }

        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            this.outputHelper.WriteLine($"Beginning S2S refesh: {targetSiteName}, {siteId}");
        }
    }
}
