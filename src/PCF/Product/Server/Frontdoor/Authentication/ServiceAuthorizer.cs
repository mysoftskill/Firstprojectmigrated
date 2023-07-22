namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    /// Validates server-server tickets. 
    /// </summary>
    public class ServiceAuthorizer : IAuthorizer
    {
        private readonly IAuthenticator stsAuthenticator;
        private readonly IDataAgentMapFactory dataAgentMapFactory;
        private readonly IDictionary<string, string> knownAgents =
            Config.Instance.Frontdoor.AuthenticatedCallers.KnownCallers.ToDictionary(caller => caller.Id, caller => caller.Name);

        /// <summary>
        /// Initializes a new instance of the class <see cref="ServiceAuthorizer" />.
        /// </summary>
        /// <param name="dataAgentMapFactory">Data agent map factory.</param>
        /// <param name="stsAuthenticator">STS Authenticator.</param>
        public ServiceAuthorizer(
            IDataAgentMapFactory dataAgentMapFactory,
            IAuthenticator stsAuthenticator)
        {
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.stsAuthenticator = stsAuthenticator;
        }

        /// <summary>
        /// Uses the venerable membershipAuthN library to check the s2s access token.
        /// </summary>
        public async Task<PcfAuthenticationContext> CheckAuthorizedAsync(HttpRequestMessage request, AgentId agentId)
        {
            if (IncomingEvent.Current != null)
            {
                IncomingEvent.Current.CallerName = agentId.GuidValue.ToString("D");
            }

            var map = this.dataAgentMapFactory.GetDataAgentMap();
            if (!map.TryGetAgent(agentId, out IDataAgentInfo dataAgentInfo))
            {
                IncomingEvent.Current?.SetProperty("AuthorizationError", "AgentIdNotFound");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Given agent ID not found");
            }

            var clientCert = await request.LoadClientCertificateAsync();
            var authContext = await this.stsAuthenticator.AuthenticateAsync(request.Headers, clientCert);

            if (authContext.AuthenticatedMsaSiteId == null && authContext.AuthenticatedAadAppId == null)
            {
                IncomingEvent.Current?.SetProperty("AuthorizationError", "InvalidScheme");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Invalid authorization scheme");
            }

            if ((authContext.AuthenticatedMsaSiteId != null && dataAgentInfo.MatchesMsaSiteId(authContext.AuthenticatedMsaSiteId.Value))
                || (authContext.AuthenticatedAadAppId != null && dataAgentInfo.MatchesAadAppId(authContext.AuthenticatedAadAppId.Value)))
            {
                return authContext;
            }

            IncomingEvent.Current?.SetProperty("AuthorizationError", "AuthenticatedIdMismatch");
            throw new AuthNException(AuthNErrorCode.InvalidTicket, $"AuthenticatedIdMismatch: Authorized ID {authContext.AuthenticatedAadAppId?.ToString() ?? authContext.AuthenticatedMsaSiteId.ToString()} does not match known ID for agent {agentId}.");
        }

        /// <inheritdoc/>
        public async Task<PcfAuthenticationContext> CheckAuthorizedAsync(HttpRequestMessage request, AuthenticationScope authenticationScope)
        {
            var clientCert = await request.LoadClientCertificateAsync();
            var authContext = await this.stsAuthenticator.AuthenticateAsync(request.Headers, clientCert);

            if (authContext.AuthenticatedMsaSiteId == null && authContext.AuthenticatedAadAppId == null)
            {
                IncomingEvent.Current?.SetProperty("AuthorizationError", "InvalidScheme");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Invalid authorization scheme");
            }

            if (IsAuthenticatedContext(authContext, authenticationScope))
            {
                if (IncomingEvent.Current != null)
                {
                    string authenticatedId = authContext.AuthenticatedMsaSiteId?.ToString() ?? authContext.AuthenticatedAadAppId.Value.ToString("D");
                    if (this.knownAgents.TryGetValue(authenticatedId, out string name))
                    {
                        IncomingEvent.Current.CallerName = name;
                    }
                    else
                    {
                        IncomingEvent.Current.CallerName = authenticatedId;
                    }
                }

                return authContext;
            }

            IncomingEvent.Current?.SetProperty("AuthorizationError", "AuthenticatedIdMismatch");
            throw new AuthNException(AuthNErrorCode.InvalidTicket, $"AuthenticatedIdMismatch: Authorized ID {authContext.AuthenticatedAadAppId?.ToString() ?? authContext.AuthenticatedMsaSiteId.ToString()} does not match any of the allowed Ids.");
        }

        private static bool IsAuthenticatedContext(PcfAuthenticationContext authenticationContext, AuthenticationScope authenticationScope)
        {
            IEnumerable<long> permissibleSiteIds = new long[0];
            IEnumerable<Guid> permissibleAppIds = new Guid[0];

            switch (authenticationScope)
            {
                case AuthenticationScope.DebugApis:
                    permissibleSiteIds = Config.Instance.Frontdoor.AuthenticatedCallers.DebugApis;
                    permissibleAppIds = Config.Instance.Frontdoor.AuthenticatedCallers.DebugApisWithAadAuth;
                    break;

                case AuthenticationScope.PxsService:
                    permissibleSiteIds = Config.Instance.Frontdoor.AuthenticatedCallers.PxsPostCommand;
                    permissibleAppIds = Config.Instance.Frontdoor.AuthenticatedCallers.PxsPostCommandWithAadAuth;
                    break;

                case AuthenticationScope.GetFullCommandStatus:
                    permissibleSiteIds = Config.Instance.Frontdoor.AuthenticatedCallers.GetFullCommandStatus;
                    permissibleAppIds = Config.Instance.Frontdoor.AuthenticatedCallers.GetFullCommandStatusWithAadAuth;
                    break;

                case AuthenticationScope.ExportStorageGetAccounts:
                    permissibleSiteIds = Config.Instance.Frontdoor.AuthenticatedCallers.ExportStorageGetAccounts;
                    permissibleAppIds = Config.Instance.Frontdoor.AuthenticatedCallers.ExportStorageGetAccountsWithAadAuth;
                    break;

                case AuthenticationScope.TestHooks:
                    permissibleSiteIds = Config.Instance.Frontdoor.AuthenticatedCallers.TestHooks;
                    permissibleAppIds = Config.Instance.Frontdoor.AuthenticatedCallers.TestHooksWithAadAuth;
                    break;
            }

            return (authenticationContext.AuthenticatedMsaSiteId != null && permissibleSiteIds.Contains(authenticationContext.AuthenticatedMsaSiteId.Value))
                || (authenticationContext.AuthenticatedAadAppId != null && permissibleAppIds.Contains(authenticationContext.AuthenticatedAadAppId.Value));
        }

        /// <summary>
        /// Try to get MSA Ticket from HttpRequestHeaders
        /// </summary>
        /// <param name="headers">HttpRequestHeaders</param>
        /// <param name="ticket">MSA ticket</param>
        /// <returns></returns>
        public static bool TryGetMsaTicket(HttpRequestHeaders headers, out string ticket)
        {
            ticket = null;

            if (headers?.Authorization?.Scheme == "MSAS2S")
            {
                ticket = headers.Authorization.Parameter;
                return true;
            }
            else if (headers != null && headers.TryGetValues("X-S2S-Access-Token", out IEnumerable<string> values))
            {
                ticket = values.FirstOrDefault();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to get AAD Token from HttpRequestHeaders
        /// </summary>
        /// <param name="headers">HttpRequestHeaders</param>
        /// <param name="token">AAD token</param>
        /// <returns></returns>
        public static bool TryGetAadToken(HttpRequestHeaders headers, out string token)
        {
            token = null;

            if (headers?.Authorization?.Scheme == "Bearer")
            {
                token = headers.Authorization.Parameter;
                return true;
            }

            return false;
        }
    }
}
