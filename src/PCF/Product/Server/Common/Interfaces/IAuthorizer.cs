namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface that can authorize data agents and other services
    /// </summary>
    public interface IAuthorizer
    {
        /// <summary>
        /// Examines the request to see if it is valid for the given agent. Throws an AuthN exception if
        /// not authorized.
        /// </summary>
        Task<PcfAuthenticationContext> CheckAuthorizedAsync(HttpRequestMessage request, AgentId agentId);

        /// <summary>
        /// Validates the auth token from the request and verifies that the AppId/siteId from it matches the permissible ones
        /// </summary>
        /// <param name="request">http request from agent</param>
        /// <param name="authenticationScope">scope to determine the permissible appsIds/siteIds</param> 
        /// <returns>The task object</returns>
        Task<PcfAuthenticationContext> CheckAuthorizedAsync(HttpRequestMessage request, AuthenticationScope authenticationScope);
    }
}
