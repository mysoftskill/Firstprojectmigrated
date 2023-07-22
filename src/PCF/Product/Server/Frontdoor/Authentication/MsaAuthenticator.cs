namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    /// Authenticates the ticket with MSA
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Lifetime object")]
    public class MsaAuthenticator : IAuthenticator
    {
        // Corresponds to an entry in rpsserver.xml.
        private const string SiteName = "SiteAuth";

        private readonly IRpsAuthServer authServer;
        private readonly IAuthenticator innerAuthenticator;

        /// <summary>
        /// Initializes a new instance of the class <see cref="MsaAuthenticator" />.
        /// </summary>
        /// <param name="authenticator">Inner authenticator to use.</param>
        public MsaAuthenticator(IAuthenticator authenticator)
        {
            // Trick to maintain hard link to RPS dll and ensure it gets copied to output directory.
            DualLogger.Instance.Information(nameof(MsaAuthenticator), "Using RPS Library = " + typeof(Microsoft.Passport.RPS.RPS).FullName);

            this.authServer = new RpsAuthServer();
            this.innerAuthenticator = authenticator;
        }

        /// <summary>
        /// Authenticates a request.
        /// </summary>
        /// <param name="requestHeaders">The request headers.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <returns>The <see cref="PcfAuthenticationContext" />.</returns>
        public async Task<PcfAuthenticationContext> AuthenticateAsync(HttpRequestHeaders requestHeaders, X509Certificate2 clientCertificate)
        {
            PcfAuthenticationContext authContext;
            if (this.innerAuthenticator == null)
            {
                authContext = new PcfAuthenticationContext();
            }
            else
            {
                authContext = await this.innerAuthenticator.AuthenticateAsync(requestHeaders, clientCertificate);
            }

            if (ServiceAuthorizer.TryGetMsaTicket(requestHeaders, out string ticket))
            {
                authContext.AuthenticatedMsaSiteId = await this.AuthenticateMsaAuthTicket(ticket, clientCertificate);
            }

            return authContext;
        }

        /// <summary>
        /// Uses the venerable membershipAuthN library to check the s2s access token.
        /// </summary>
        private async Task<long> AuthenticateMsaAuthTicket(string ticket, X509Certificate2 clientCert)
        {
            IncomingEvent.Current?.SetProperty("S2SAuthProvider", "MSA");

            if (string.IsNullOrEmpty(ticket))
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "MissingTicket");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Missing ticket");
            }

            if (clientCert == null)
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "MissingCertificate");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Missing client certificate");
            }

            // Blocks the current thread, so shove it onto the threadpool.
            RpsAuthResult authResult = await Task.Run(() => this.authServer.GetS2SSiteAuthResult(SiteName, ticket, clientCert.GetRawCertData()));

            if (authResult?.AppId == null)
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "MissingAppId");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Missing AppID in ticket");
            }

            long siteId = authResult.AppId.Value;
            IncomingEvent.Current?.SetProperty("AuthorizedId", siteId.ToString());

            return siteId;
        }
    }
}
