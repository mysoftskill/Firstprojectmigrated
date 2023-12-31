namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.Windows.Services.AuthN.Server;
    using Newtonsoft.Json;

#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// Does delegated authentication via the stress header.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [ExcludeFromCodeCoverage]
    internal class StressDelegatedAuthenticator : IAuthenticator
    {
        public Task<PcfAuthenticationContext> AuthenticateAsync(HttpRequestHeaders requestHeaders, X509Certificate2 clientCertificate)
        {
            if (requestHeaders.TryGetValues(StressRequestForwarder.StressDelegatedAuthHeader, out var values))
            {
                string firstHeader = values.First();
                PcfAuthenticationContext context = JsonConvert.DeserializeObject<PcfAuthenticationContext>(firstHeader);
                return Task.FromResult(context);
            }
            else if (requestHeaders.TryGetValues("X-Stress-Delegated-Auth", out values))
            {
                // Temporary support contract changes to not break Stress.
                string firstHeader = values.First();
                dynamic context = JsonConvert.DeserializeObject(firstHeader);

                var newContext = new PcfAuthenticationContext
                {
                    AuthenticatedAadAppId = context.AuthenticatedAadAppIds.First,
                    AuthenticatedMsaSiteId = context.AuthenticatedMsaSiteIds.First,
                };

                return Task.FromResult(newContext);
            }
            else
            {
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Couldn't find PCF stress header.");
            }
        }
    }
#endif
}
