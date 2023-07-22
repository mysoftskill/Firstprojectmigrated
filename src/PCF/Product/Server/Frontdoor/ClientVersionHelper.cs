
namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;

    public static class ClientVersionHelper
    {
        private const string ClientVersionHeader = "x-client-version";

        private const string RegexPattern = @"pcfsdk;(.*?);";

        private const string MinimumPcfClientVersionSupportsMultiTenantCollaboration = "1.6.1705.15";

        private static readonly Regex clientVersionRegex = new Regex(RegexPattern, RegexOptions.Compiled);

        /// <summary>
        /// Gets PCF client/sdk version string from HTTP request header
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>The client version if exists, null otherwise</returns>
        public static string GetClientVersionHeader(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(ClientVersionHeader, out IEnumerable<string> clientVersionHeaderValues))
            {
                return clientVersionHeaderValues.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets PCF client/sdk version (pcfsdk part removed) from version string
        /// </summary>
        /// <param name="clientVersionHeader">The Pcf client version string extracted from HTTP request header</param>
        /// <returns>The client version, e.g. "1.5.1684.14", if exists, null otherwise</returns>
        public static string GetClientVersionNumber(string clientVersionHeader)
        {
            if (string.IsNullOrWhiteSpace(clientVersionHeader))
            {
                return null;
            }

            Match extractedClientVersion = clientVersionRegex.Match(clientVersionHeader);

            return (extractedClientVersion.Success && extractedClientVersion.Groups.Count > 1) ? extractedClientVersion.Groups[1].ToString() : null;
        }

        /// <summary>
        /// Gets PCF client/sdk version (pcfsdk part removed) from HTTP request header
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>The client version, e.g. "1.5.1684.14", if exists, null otherwise</returns>
        public static string GetClientVersionNumber(HttpRequestMessage request)
        {
            return GetClientVersionNumber(GetClientVersionHeader(request));
        }

        /// <summary>
        /// Checks if the PCF client/sdk version from HTTP request header meets the minimum requirement for multi-tenant collaboration
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>True - supports multi-tenant collaboration, false otherwise</returns>
        public static bool DoesClientSupportMultiTenantCollaboration(HttpRequestMessage request)
        {
            var clientVersion = GetClientVersionNumber(request);
            return !string.IsNullOrWhiteSpace(clientVersion) && string.Compare(clientVersion, MinimumPcfClientVersionSupportsMultiTenantCollaboration, StringComparison.Ordinal) >= 0;
        }
    }
}
