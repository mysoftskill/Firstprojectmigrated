namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using System.Runtime.CompilerServices;

    public class ApiTrafficHandler : IApiTrafficHandler
    {
        private static readonly string ComponentName = nameof(ApiTrafficHandler);

        private static readonly int DefaultRetryTimeInSeconds = 5;

        /// <summary>
        /// The key to the config that stores the percentage value for all traffic throttling
        /// </summary>
        private static readonly string TrafficKeyForAllApisAgentsAssetGroups = "*.*.*";

        private readonly HttpResponseMessage tooManyrequestsResponse;

        public ApiTrafficHandler() 
        {
            this.tooManyrequestsResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("Too Many Requests. Retry later with suggested delay in retry header."),
                Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(DefaultRetryTimeInSeconds)) }
            };
        }

        /// <summary>
        /// Check if the incoming traffic for a given agent, a given api (and a given assetGroup if there is any) should be allowed.
        /// </summary>
        /// <param name="trafficPercentageConfigName">The app config that stores a collection of traffic percentages.</param>
        /// <param name="apiName">The api name or the operation name.</param>
        /// <param name="agentId">The agent id. Guid without dashes.</param>
        /// <param name="assetGroupId">The asset group id. Guid without dashes. 
        /// Optional. Currently only QueryCommandByCommandId API provides asset group id information. </param>
        /// <returns>A boolean value that indicates whether the incoming request should be allowed. Returns true if allowed.</returns>
        public bool ShouldAllowTraffic(string trafficPercentageConfigName, string apiName, string agentId, string assetGroupId = "*")
        {
            bool IsRequestAllowed;
            try
            {
                var apiTrafficPercentageList = FlightingUtilities.GetConfigValues<ApiTrafficPercentageFromJson>(trafficPercentageConfigName);

                // Checks if the config has any records at all
                if (apiTrafficPercentageList.Length == 0)
                {
                    // No need to throttle
                    IsRequestAllowed = true;
                }

                IsRequestAllowed = Execute(
                    apiTrafficPercentageList.ToDictionary(a => a.TrafficKey, a => a.Percentage, StringComparer.OrdinalIgnoreCase),
                    String.Join(".", apiName, agentId, assetGroupId),
                    RandomHelper.NextDouble() * 100);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(ComponentName, ex, $"Unexpected exception while executing api traffic throttling. Error Message={ex.Message}");
                IsRequestAllowed = true;
            }

            IncomingEvent.Current?.SetProperty("RequestAllowed", IsRequestAllowed.ToString());
            return IsRequestAllowed;
        }

        public HttpResponseMessage GetTooManyRequestsResponse()
        {
            // return with 429 and retry header
            return this.tooManyrequestsResponse;
        }

        /// <summary>
        /// Decides whether the throttling should be applied to all traffic or given traffic
        /// Decides to throttle the traffic or not based on random value comparison 
        /// </summary>
        /// <param name="apiTrafficPercentageDict">Percentage values retrievd from the app config</param>
        /// <param name="trafficKeyToSearchFor">Traffic key for given traffic</param>
        /// <param name="randomValue">A random generated value from 0-100</param>
        /// <returns>A boolean value that indicates whether the incoming request should be allowed. Returns true if allowed.</returns>
        private static bool Execute(IDictionary<string, string> apiTrafficPercentageDict, string trafficKeyToSearchFor, double randomValue)
        {
            // The percentage is less than 100 means the throttling should apply to all the traffic
            if (apiTrafficPercentageDict.TryGetValue(key: TrafficKeyForAllApisAgentsAssetGroups, out string PercentageValForAllTrafficFromConfig)
                && Convert.ToInt32(PercentageValForAllTrafficFromConfig) < 100)
            {
                // Should apply to all apis and agents
                return Convert.ToInt32(PercentageValForAllTrafficFromConfig) >= randomValue;
            }
            else
            {
                // Check if the throttling should apply to the given api, agentId and assetGroupId
                if (apiTrafficPercentageDict.TryGetValue(key: trafficKeyToSearchFor, out string PercentageValFromConfig))
                {
                    return Convert.ToInt32(PercentageValFromConfig) >= randomValue;
                }
                else
                {
                    // No limit added for this traffic
                    return true;
                }
            }
        }
    }
}
