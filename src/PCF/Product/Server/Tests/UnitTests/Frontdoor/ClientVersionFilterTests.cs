namespace PCF.UnitTests.Frontdoor
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;

    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ClientVersionFilterTests
    {
        [Fact]
        public async Task ValidVersionHeader()
        {
            var response = await this.ExecuteRequestWithHeader($"pcfsdk;{ClientVersionFilterAttribute.MinimumPcfClientVersion};v:true");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ValidHigherVersionHeader()
        {
            string version = ClientVersionFilterAttribute.MinimumPcfClientVersion;
            string[] versionParts = version.Split('.');
            var buildDate = int.Parse(versionParts[2]);
            buildDate++;
            versionParts[2] = buildDate.ToString();
            version = string.Join(".", versionParts);
            var response = await this.ExecuteRequestWithHeader($"pcfsdk;{version};v:true");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OutdatedValidVersionHeader()
        {
            var response = await this.ExecuteRequestWithHeader("pcfsdk;1.0.0.0;v:true");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NotMatchingRegexPatternVersionHeader()
        {
            var response = await this.ExecuteRequestWithHeader("badclientheader");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task EmptyValidVersionHeader()
        {
            var response = await this.ExecuteRequestWithHeader(string.Empty);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task NoVersionHeader()
        {
            var filter = new ClientVersionFilterAttribute();
            var context = new HttpActionContext
            {
                ControllerContext = new HttpControllerContext
                {
                    Request = new HttpRequestMessage()
                }
            };
            var response = await filter.ExecuteActionFilterAsync(context, CancellationToken.None, () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<HttpResponseMessage> ExecuteRequestWithHeader(string clientVersionHeaderValue)
        {
            var filter = new ClientVersionFilterAttribute();

            var request = new HttpRequestMessage();
            request.Headers.Add("x-client-version", clientVersionHeaderValue);

            var context = new HttpActionContext
            {
                ControllerContext = new HttpControllerContext
                {
                    Request = request
                }
            };
            return await filter.ExecuteActionFilterAsync(context, CancellationToken.None, () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }
    }
}
