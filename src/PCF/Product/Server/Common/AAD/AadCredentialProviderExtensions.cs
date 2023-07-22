namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Rest;
    
    /// <summary>
    /// Extensions for the <see cref="IAadCredentialProvider"/> interface.
    /// </summary>
    public static class AadCredentialProviderExtensions
    {
        /// <summary>
        /// Helper method to get Azure management credentials.
        /// </summary>
        public static Task<TokenCredentials> GetAzureManagementTokenCredentialsAsync(this IAadCredentialProvider provider)
        {
            return provider.GetCredentialsAsync("https://management.azure.com/");
        }
    }
}
