namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;
    using Newtonsoft.Json;
    using Xunit;

#if INCLUDE_TEST_HOOKS

    [Trait("Category", "UnitTest")]
    public class StressDelegateAuthenticatorTests
    {
        [Fact]
        public async void StressDelegateAuthenticatorThrowsIfNoHeaders()
        {
            var request = new HttpRequestMessage();

            var authenticator = new StressDelegatedAuthenticator();

            var authError = new AuthNException(AuthNErrorCode.None);
            try
            {
                var authContext = await authenticator.AuthenticateAsync(request.Headers, null);
            }
            catch (AuthNException ex)
            {
                authError = ex;
            }

            Assert.Equal(AuthNErrorCode.InvalidTicket, authError.ErrorCode);
            Assert.Equal("Couldn't find PCF stress header.", authError.Message);
        }

        [Fact]
        public async void StressDelegateAuthenticatorSucceedsWithGoodHeaders()
        {
            var appId = Guid.NewGuid();
            var pcfAuthContext = new PcfAuthenticationContext { AuthenticatedAadAppId = appId };

            var request = new HttpRequestMessage();
            request.Headers.Add("X-Stress-Delegated-Authentication", JsonConvert.SerializeObject(pcfAuthContext));

            var authenticator = new StressDelegatedAuthenticator();

            var authContext = await authenticator.AuthenticateAsync(request.Headers, null);

            Assert.Equal(appId, authContext.AuthenticatedAadAppId);
        }

        [Fact]
        public async void StressDelegateAuthenticatorShimTest()
        {
            var appId = Guid.NewGuid();

            var pcfAuthContext = new AuthContext { AuthenticatedAadAppIds = new List<Guid> { appId } };
            string value = JsonConvert.SerializeObject(pcfAuthContext);

            var request = new HttpRequestMessage();
            request.Headers.Add("X-Stress-Delegated-Auth", value);

            var authenticator = new StressDelegatedAuthenticator();

            var authContext = await authenticator.AuthenticateAsync(request.Headers, null);

            Assert.Equal(appId, authContext.AuthenticatedAadAppId);
        }

        public class AuthContext
        {
            /// <summary>
            /// Set of AAD App IDs that have been authenticated for this request.
            /// </summary>
            public IList<Guid> AuthenticatedAadAppIds { get; set; } = new Guid[0];

            /// <summary>
            /// Set of MSA Site IDs that have been authenticated for this request.
            /// </summary>
            public IList<long> AuthenticatedMsaSiteIds { get; set; } = new long[0];
        }
    }
#endif
}