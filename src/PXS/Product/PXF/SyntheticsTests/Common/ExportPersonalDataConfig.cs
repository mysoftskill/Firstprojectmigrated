namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;

    /// <summary>
    /// Description of the configuration of an AzureAD public client application (desktop/mobile application). This should
    /// match the application registration done in the Azure portal
    /// </summary>
    public class ExportPersonalDataConfig
    {
        /// <summary>
        /// Graph API endpoint, could be public Azure (default) or a Sovereign cloud (US government, etc ...)
        /// </summary>
        public string ApiUrlEndpoint { get; set; } = "https://graph.microsoft.com/v1.0/";

        /// <summary>
        /// Graph API Path template (the format of the path to the ExportPersonalData action)
        /// </summary>
        public string ApiPathTemplate { get; set; } = "users/{0}/exportPersonalData";

        /// <summary>
        /// Guid used by the application to uniquely identify itself to Azure AD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Name of user making the request
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password for UserName
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Blob Storage Connection String
        /// </summary>
        public string BlobStorageConnectionString { get; set; }

        /// <summary>
        /// Authority URL for getting access token
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Resource Tenant Id
        /// </summary>
        public string ResourceTenantId { get; set; }

        private KeyVaultClient keyVaultClient;

        public ExportPersonalDataConfig(TelemetryClient telemetryClient, IReadOnlyDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            ApiUrlEndpoint = GetStringValue(telemetryClient, parameters, "ApiUrlEndpoint");
            ApiPathTemplate = GetStringValue(telemetryClient, parameters, "ApiPathTemplate");
            ClientId = GetStringValue(telemetryClient, parameters, "ClientId");
            Authority = GetStringValue(telemetryClient, parameters, "Authority");
            // Extract the paramaters needed from azure keyvault
            string keyVaultUrl = parameters["KeyVaultUrl"];

            // Create a keyvault client using msi
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            // Get User and Storage information needed to submit an export request
            try
            {
                UserName = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["UserUpnSecretName"]).GetAwaiter().GetResult().Value;
                Password = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["UserPasswordSecretName"]).GetAwaiter().GetResult().Value;
                BlobStorageConnectionString = keyVaultClient.GetSecretAsync(keyVaultUrl, parameters["BlobStorageConnectionStringSecretName"]).GetAwaiter().GetResult().Value;
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


    }
}


