namespace Microsoft.PrivacyServices.Common.Cosmos
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;

    public class CosmosResourceFactory: ICosmosResourceFactory
    {
        /// <inheritdoc/>
        public ICosmosClient CreateCosmosAdlsClient(AdlsConfig config)
        {
            return new CosmosAdlsClient(config, GetAppToken(config).GetAwaiter().GetResult(), GetAppToken);
        }

        public async Task<string> GetAppToken(AdlsConfig config)
        {
            var scopes = new[] { $"https://datalake.azure.net//.default" };

            var authResult = await ConfidentialCredential.GetTokenAsync(
                config.ClientAppId,
                config.Cert,
                new Uri(string.Format($"https://login.microsoftonline.com/{config.TenantId}")),
                scopes
            ).ConfigureAwait(false);

            if (authResult == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return authResult.AccessToken;
        }
    }
}
