// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <inheritdoc />
    public class AadTokenManager : ITokenManager
    {
        private static ConcurrentDictionary<string, ConfidentialCredential> confidentialClientMapping = new ConcurrentDictionary<string, ConfidentialCredential>();

        /// <inheritdoc />
        public async Task<string> GetAppTokenAsync(string authority, string clientId, string resource, X509Certificate2 certificate, bool cacheable = true, ILogger logger = null)
        {
            string[] scopes = new string[] { $"{resource}/.default" };

            StringBuilder sb = new StringBuilder(clientId, 200);

            sb.Append(authority);
            sb.Append(certificate.Thumbprint);
            string objectInstanceLookupKey = sb.ToString();

            AuthenticationResult result;
            try
            {
                if (cacheable)
                {
                    if (confidentialClientMapping.TryGetValue(objectInstanceLookupKey, out ConfidentialCredential credentialclient))
                    {
                        result = await credentialclient.GetTokenAsync(scopes);
                    }
                    else
                    {
                        var newCredentialClient = new ConfidentialCredential(clientId, certificate, new Uri(authority), logger);
                        result = await newCredentialClient.GetTokenAsync(scopes);

                        // Limits the total number of confidential clients in case future implementation adds a many different connections
                        if (confidentialClientMapping.Count < 200)
                        {
                            confidentialClientMapping.TryAdd(objectInstanceLookupKey, newCredentialClient);
                        }
                    }
                }
                else
                {
                    result = await ConfidentialCredential.GetTokenAsync(clientId, certificate, new Uri(authority), scopes, logger);
                }

                return result.AccessToken;
            }
            catch (Exception e)
            {
                logger?.Error("AadTokenManager", e, "Exception from GetAppTokenAsync");
                Trace.TraceError($"[{nameof(AadTokenManager)}]: {e}");
                throw;
            }
        }
    }
}
