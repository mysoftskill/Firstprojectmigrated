namespace PCF.UnitTests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AuthTokenTests
    {
        [Theory]
        [InlineData("Bearer", "AadToken", true)]
        [InlineData("MSAS2S", null, false)]
        public void TryGetAadToken(string scheme, string token, bool expected)
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);

            var result = ServiceAuthorizer.TryGetAadToken(request.Headers, out string tokenResult);

            Assert.Equal(expected, result);
            Assert.Equal(token, tokenResult);
        }

        [Theory]
        [InlineData("MSAS2S", "MsaTicket", "DoesNotMatter", new[] { "" }, true)]
        [InlineData("Bearer", "MsaTicket", "X-S2S-Access-Token", new[] { "MsaTicket", "BadMsaTicket" }, true)]
        [InlineData("Bearer", null, "DoesNotMatter", new[] { "MsaTicket", "BadMsaTicket" }, false)]
        public void TryGetMsaToken(string scheme, string ticket, string s2sTokenName, string[] tokens, bool expected)
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, ticket);
            request.Headers.Add(s2sTokenName, tokens);

            var result = ServiceAuthorizer.TryGetMsaTicket(request.Headers, out string ticketResult);

            Assert.Equal(expected, result);
            Assert.Equal(ticket, ticketResult);
        }
    }
}