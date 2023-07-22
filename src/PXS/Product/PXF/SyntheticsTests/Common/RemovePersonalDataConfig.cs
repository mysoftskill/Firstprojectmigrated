namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Membership.MemberServices.Common.Azure;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config;
    using Microsoft.Membership.MemberServices.Test.Common;

    /// <summary>
    /// Description of the configuration of an AzureAD public client application (desktop/mobile application). This should
    /// match the application registration done in the Azure portal
    /// </summary>
    public class RemovePersonalDataConfig
    {
        /// <summary>
        /// Graph API endpoint, could be public Azure (default) or a Sovereign cloud (US government, etc ...)
        /// </summary>
        public string ApiUrlEndpoint { get; set; } = "https://graph.microsoft.com/v1.0/";

        /// <summary>
        /// Graph API Path template (the format of the path to the ExportPersonalData action)
        /// </summary>
        public string ApiPathTemplate { get; set; } = "inboundSharedUserProfiles/{0}/removePersonalData";

        /// <summary>
        /// Guid used by the application in tenant meepxs.onmicrosoft.com to uniquely identify itself to Azure AD
        /// </summary>
        public string HomeTenantClientId { get; set; }

        /// <summary>
        /// Guid used by the application in tenant meepxsresource.onmicrosoft.com to uniquely identify itself to Azure AD
        /// </summary>
        public string ResourceTenantClientId { get; set; }

        /// <summary>
        /// Name of user making the request from tenant meepxs.onmicrosoft.com
        /// </summary>
        public string UserNameForHomeTenant { get; set; }

        /// <summary>
        /// Password For User in tenant meepxs.onmicrosoft.com
        /// </summary>
        public string PasswordForUserInHomeTenant { get; set; }

        /// <summary>
        /// Name of user making the request from tenant meepxsresource.onmicrosoft.com
        /// </summary>
        public string UserNameForResourceTenant { get; set; }

        /// <summary>
        /// Password For User in tenant meepxsresource.onmicrosoft.com
        /// </summary>
        public string PasswordForUserInResourceTenant { get; set; }

        private IKeyVaultClient keyVaultClient;

        public RemovePersonalDataConfig(TelemetryClient telemetryClient, IReadOnlyDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            ApiUrlEndpoint = GetStringValue(telemetryClient, parameters, "ApiUrlEndpoint");
            ApiPathTemplate = GetStringValue(telemetryClient, parameters, "ApiPathTemplate");
            HomeTenantClientId = GetStringValue(telemetryClient, parameters, "HomeTenantClientId");
            ResourceTenantClientId = GetStringValue(telemetryClient, parameters, "ResourceTenantClientId");
            // Extract the paramaters needed from azure keyvault
            string keyVaultUrl = parameters["KeyVaultUrl"];

            // Create a keyvault client using msi
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            // Get User and Storage information needed to submit an export request
            try
            {
                UserNameForHomeTenant = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["User1UpnSecretName"]).GetAwaiter().GetResult().Value;
                PasswordForUserInHomeTenant = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["User1PasswordSecretName"]).GetAwaiter().GetResult().Value;
                UserNameForResourceTenant = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["User2UpnSecretName"]).GetAwaiter().GetResult().Value;
                PasswordForUserInResourceTenant = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["User2PasswordSecretName"]).GetAwaiter().GetResult().Value;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Get the value associated with key
        /// </summary>
        /// <param name="telemetryClient">Telemetry client for logging</param>
        /// <param name="parameters">Parameters to search for key</param>
        /// <param name="key">Value to retrieve</param>
        /// <returns>Value associated with key.</returns>
        protected string GetStringValue(TelemetryClient telemetryClient, IReadOnlyDictionary<string, string> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out string value))
            {
                telemetryClient.TrackTrace($"Parameter {key} not defined, cannot run test", SeverityLevel.Critical);
            }

            return value;
        }
        private static async Task<string> GetAccessTokenAsync(string authority, string resource, string scope = null)
        {

            string[] scopes = new string[] { $"{resource}/.default" };
            var result = await ConfidentialCredential.GetTokenAsync(TestData.TestAadAppId, TestConfiguration.S2SCert.Value, new Uri(authority), scopes);
            return result?.AccessToken ?? throw new InvalidOperationException("Failed to get AAD JWT token");
        }
    }
}


