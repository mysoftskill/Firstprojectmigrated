// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Identity;

    /// <summary>
    /// Managed Service Identity (MSI) token provider.
    /// </summary>
    public class MsiAppTokenProvider : IAppTokenProvider
    {
        private readonly string clientId;
        private readonly string resource;

        /// <summary>
        /// Create MsiAppTokenProvider.
        /// </summary>
        /// <param name="clientId">Managed identity client id.</param>
        /// <param name="resource">The resource</param>
        public MsiAppTokenProvider(
            string clientId, 
            string resource)
        {
            this.clientId = clientId;
            this.resource = resource;
        }

        /// <inheritdoc />
        public async Task<string> GetAppTokenAsync()
        {
            var tokenCredential = new ManagedIdentityCredential(this.clientId);
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { this.resource + "/.default" }) { }
            ).ConfigureAwait(false);

            return accessToken.Token;
        }
    }
}
