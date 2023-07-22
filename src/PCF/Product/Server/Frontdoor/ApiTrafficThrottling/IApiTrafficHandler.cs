using System.Net.Http;

namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling
{
    public interface IApiTrafficHandler
    {
        /// <summary>
        /// Check if the incoming traffic for a given agent, a given api (and a given assetGroup if there is any) should be allowed.
        /// </summary>
        /// <param name="trafficPercentageConfigName">The app config that stores a collection of traffic percentages.</param>
        /// <param name="apiName">The api name or the operation name.</param>
        /// <param name="agentId">The agent id. Guid without dashes.</param>
        /// <param name="assetGroupId">The asset group id. Guid without dashes. 
        /// Optional. Currently only QueryCommandByCommandId API provides asset group id information. </param>
        /// <returns>A boolean value that indicates whether the incoming request should be allowed. Returns true if allowed.</returns>
        bool ShouldAllowTraffic(string trafficPercentageConfigName, string apiName, string agentId, string assetGroupId = "*");

        /// <summary>
        /// Returns a 429 response with a retry header
        /// </summary>
        /// <returns></returns>
        HttpResponseMessage GetTooManyRequestsResponse();
    }
}
