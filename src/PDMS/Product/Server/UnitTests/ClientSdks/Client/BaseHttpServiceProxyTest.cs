namespace Microsoft.PrivacyServices.DataManagement.Client.UnitTest
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;

    using Moq;
    using Xunit;

    public class BaseHttpServiceProxyTest
    {
        [Fact(DisplayName = "When the service proxy is disposed, then dispose of the http client.")]
        public async Task VerifyDisposeFreesHttpClient()
        {
            var client = new HttpClient();

            var proxy = new Mock<BaseHttpServiceProxy>(client) { CallBase = true };
            proxy.Object.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetAsync("api")).ConfigureAwait(false);
        }

        [Fact(DisplayName = "When the service proxy is disposed repeatedly, then do not fail.")]
        public void VerifyDisposeRepeatedly()
        {
            var client = new HttpClient();
            var proxy = new Mock<BaseHttpServiceProxy>(client);
            proxy.Object.Dispose();
            proxy.Object.Dispose();
        }

        [Theory(DisplayName = "Verify operation name is set properly for GET.")]
        [InlineData("/api/v2/dataOwners('9473986C-46EA-4948-B83D-752B5723B3BA')", "V2.DataOwners.Read")]
        [InlineData("/probe", "Probe")]
        public async Task VerifySettingOperationNameGet(string url, string operationName)
        {
            var handler = new MockHandler(r => Assert.Equal(operationName, r.Properties["OperationNameKey"]));

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://bing.com/");

            var proxy = new Mock<BaseHttpServiceProxy>(client);

            var response = await proxy.Object.GetAsync<int>(url, null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(operationName, response.OperationName);
        }

        [Theory(DisplayName = "Verify operation name is set properly for PUT.")]
        [InlineData("/api/v2/dataOwners('9473986C-46EA-4948-B83D-752B5723B3BA')", "V2.DataOwners.Update")]
        public async Task VerifySettingOperationNamePut(string url, string operationName)
        {
            var handler = new MockHandler(r => Assert.Equal(operationName, r.Properties["OperationNameKey"]));

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://bing.com/");

            var proxy = new Mock<BaseHttpServiceProxy>(client);

            var response = await proxy.Object.PutAsync<int, int>(url, 0, null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(operationName, response.OperationName);
        }

        [Theory(DisplayName = "Verify operation name is set properly for POST.")]
        [InlineData("/api/v2/dataOwners", "V2.DataOwners.Create")]
        public async Task VerifySettingOperationNamePost(string url, string operationName)
        {
            var handler = new MockHandler(r => Assert.Equal(operationName, r.Properties["OperationNameKey"]));

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://bing.com/");

            var proxy = new Mock<BaseHttpServiceProxy>(client);

            var response = await proxy.Object.PostAsync<int, int>(url, 0, null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(operationName, response.OperationName);
        }

        [Theory(DisplayName = "Verify operation name is set properly for DELETE.")]
        [InlineData("/api/v2/assetGroups('9473986C-46EA-4948-B83D-752B5723B3BA')", "V2.AssetGroups.Delete")]
        public async Task VerifySettingOperationNameDelete(string url, string operationName)
        {
            var handler = new MockHandler(r => Assert.Equal(operationName, r.Properties["OperationNameKey"]));

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://bing.com/");

            var proxy = new Mock<BaseHttpServiceProxy>(client);

            var response = await proxy.Object.DeleteAsync(url, null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(operationName, response.OperationName);
        }

        private class MockHandler : HttpMessageHandler
        {
            private readonly Action<HttpRequestMessage> verify;

            public MockHandler(Action<HttpRequestMessage> verify)
            {
                this.verify = verify;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.verify(request);
                var response = new HttpResponseMessage();
                response.Content = new StringContent("1");
                return Task.FromResult(response);
            }
        }
    }
}