// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;
    using Newtonsoft.Json;

    /// <summary>
    ///     GraphAuthenticationHelper
    /// </summary>
    public static class GraphAuthenticationHelper
    {
        /// <summary>
        ///     Gets a graph access token
        /// </summary>
        /// <param name="username">username. Ex: user@tenantname.onmicrosoft.com</param>
        /// <param name="password">password</param>
        /// <param name="clientId">3rd party app id for creating Graph tokens</param>
        /// <param name="scope">scope of permission</param>
        /// <returns>access token</returns>
        public static async Task<string> GetGraphAccessTokenAsync(string username, string password, string clientId, string authority = "https://login.microsoftonline.com", string scope = "Directory.AccessAsUser.All")
        {
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "password", password },
                { "grant_type", "password" }, 
                { "username", username },
                { "scope", scope }
            };

            var content = new FormUrlEncodedContent(values);

            HttpClient client = new HttpClient(); // lgtm [cs/httpclient-checkcertrevlist-disabled]
            string accessTokenUri = $"{authority}/{ExtractTenantNameFromUpn(username)}/oauth2/v2.0/token";
            HttpResponseMessage getTokenResponse = await client.PostAsync(
                new Uri(accessTokenUri),
                content).ConfigureAwait(false);

            if (getTokenResponse.IsSuccessStatusCode)
            {
                string rawEvoResponse = await getTokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                dynamic evoResponse = JsonConvert.DeserializeObject(rawEvoResponse);

                return evoResponse.access_token;
            }

            throw new HttpException((int)getTokenResponse.StatusCode, await getTokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        private static string ExtractTenantNameFromUpn(string userPrincipalname)
        {
            if (string.IsNullOrWhiteSpace(userPrincipalname))
            {
                throw new ArgumentException($"{nameof(userPrincipalname)} must be set before calling {nameof(ExtractTenantNameFromUpn)}");
            }

            return userPrincipalname.Substring(userPrincipalname.LastIndexOf('@') + 1);
        }
    }
}
