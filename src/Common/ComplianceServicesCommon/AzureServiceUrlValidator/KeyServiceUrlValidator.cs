namespace Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public static class KeyServiceUrlValidator
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        private static readonly Dictionary<string, string> s_envKeyVaultZoneSuffixMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "public", ".vault.azure.net" },
            { "fairfax", ".vault.usgovcloudapi.net" },
            { "mooncake", ".vault.azure.cn" },
            { "usnat", ".vault.cloudapi.eaglex.ic.gov" },
            { "ussec", ".vault.cloudapi.microsoft.scloud" }
        };

        private static readonly Dictionary<string, string> s_envMhsmZoneSuffixMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "public", ".managedhsm.azure.net" },
            { "fairfax", ".managedhsm.usgovcloudapi.net" }
        };

        public static async Task<KeyServiceUriValidationResult> IsValidKeyServiceUrlAsync(Uri keyServiceUri, string environment, Guid? expectedTenantId = null)
        {
            KeyServiceUriValidationResult result = IsValidKeyServiceUrl(keyServiceUri, environment);
            if (result.IsValid && expectedTenantId.HasValue)
            {
                result = await CheckTenantOfAkvAsync(keyServiceUri, environment, expectedTenantId.Value);
            }

            return result;
        }

        private static KeyServiceUriValidationResult IsValidKeyServiceUrl(Uri keyServiceUri, string environment)
        {
            if (keyServiceUri == null)
            {
                throw new ArgumentNullException(nameof(keyServiceUri));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (!keyServiceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Uri {keyServiceUri} is not an absolute uri", nameof(keyServiceUri));
            }

            if (keyServiceUri.Scheme != Uri.UriSchemeHttps)
            {
                return new KeyServiceUriValidationResult(false, "{keyServiceUri} is not using https.");
            }

            string host = keyServiceUri.Host;
            if ((s_envKeyVaultZoneSuffixMapping.TryGetValue(environment, out string keyVaultSubdomain)
                && host.EndsWith(keyVaultSubdomain, StringComparison.OrdinalIgnoreCase)) ||
                (s_envMhsmZoneSuffixMapping.TryGetValue(environment, out string mhsmSubDomain)
                && host.EndsWith(mhsmSubDomain, StringComparison.OrdinalIgnoreCase)))
            {
                return new KeyServiceUriValidationResult(true, $"{keyServiceUri} matched expected resource URI for environment.");
            }

            return new KeyServiceUriValidationResult(false, $"{keyServiceUri} did not match expected resource URI for environment.");
        }

        private static async Task<KeyServiceUriValidationResult> CheckTenantOfAkvAsync(Uri keyServiceUri, string environment, Guid expectedTenantId)
        {
            if (keyServiceUri == null)
            {
                throw new ArgumentNullException(nameof(keyServiceUri));
            }

            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            HttpResponseMessage responseMessage = await s_httpClient.GetAsync(keyServiceUri);
            if (responseMessage.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                return new KeyServiceUriValidationResult(false, $"Invalid status code {responseMessage.StatusCode} received from endpoint {keyServiceUri} for auth challenge");
            }

            if (!responseMessage.Headers.TryGetValues("WWW-Authenticate", out IEnumerable<string> authenticateHeaderValues) ||
                !authenticateHeaderValues.Any())
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header is missing in response from endpoint {keyServiceUri}");
            }

            string headerValue = authenticateHeaderValues.FirstOrDefault();
            if (string.IsNullOrEmpty(headerValue))
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header is empty in response from endpoint {keyServiceUri}");
            }

            if (!AuthenticationHeaderValue.TryParse(headerValue, out AuthenticationHeaderValue authenticationHeaderValue) ||
                string.IsNullOrEmpty(authenticationHeaderValue.Parameter))
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header is invalid in response from endpoint {keyServiceUri}. header value: {headerValue}");
            }

            string parametersString = authenticationHeaderValue.Parameter;
            string resourceUriString = null;
            string authUriString = null;
            foreach (string parameterString in parametersString.Split(','))
            {
                string[] keyValue = parameterString.Split(new char[] { '=' }, 2);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(keyValue[0]))
                {
                    continue;
                }

                if ("resource".Equals(keyValue[0].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(keyValue[1]))
                {
                    resourceUriString = keyValue[1].Trim('\"');
                }
                else if ("authorization".Equals(keyValue[0].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(keyValue[1]))
                {
                    authUriString = keyValue[1].Trim('\"');
                }
            }

            if (!Uri.TryCreate(resourceUriString, UriKind.Absolute, out Uri resourceUri) ||
                !Uri.TryCreate(authUriString, UriKind.Absolute, out Uri authUri))
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header is invalid in response from endpoint {keyServiceUri}. header value: {headerValue}");
            }

            string tenantId = authUri.PathAndQuery.Trim('/');
            if (!Guid.TryParse(tenantId, out Guid tenantIdGuid))
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header has invalid tenantId response from endpoint {keyServiceUri}. header value: {headerValue}");
            }

            if (tenantIdGuid != expectedTenantId)
            {
                return new KeyServiceUriValidationResult(false, $"WWW-Authenticate header has different tenantId {tenantIdGuid} in response from endpoint {keyServiceUri}, expected {expectedTenantId}. header value: {headerValue}");
            }

            string tempVaultHost = "." + resourceUri.Host;

            if ((s_envKeyVaultZoneSuffixMapping.TryGetValue(environment, out string keyVaultSubdomain)
                && tempVaultHost.Equals(keyVaultSubdomain, StringComparison.OrdinalIgnoreCase)) ||
                (s_envMhsmZoneSuffixMapping.TryGetValue(environment, out string mhsmSubDomain)
                && tempVaultHost.Equals(mhsmSubDomain, StringComparison.OrdinalIgnoreCase)))
            {
                return new KeyServiceUriValidationResult(true, $"Expected resource URI for environment and tenantId {expectedTenantId} were matched using {keyServiceUri}");
            }

            return new KeyServiceUriValidationResult(false, $"{keyServiceUri} did not match expected resource URI for environment.");
        }
    }

    public class KeyServiceUriValidationResult
    {
        public KeyServiceUriValidationResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public bool IsValid
        {
            get;
            private set;
        }

        public string Reason
        {
            get;
            private set;
        }
    }
}
