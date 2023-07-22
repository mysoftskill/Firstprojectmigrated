namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient
{
    using System.Threading.Tasks;

    /// <summary>
    /// AAD auth token for key vault call back
    /// </summary>
    public interface IAzureKeyVaultAccessToken
    {
        /// <summary>
        /// Tenant ID where the token was acquired from
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// The App ID used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if an App Id was not used to acquire the token.</remarks>
        string AppId { get; }

        /// <summary>
        /// The CertificateThumbprint used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if a CertificateThumbprint was not used to acquire the token.</remarks>
        string CertificateThumbprint { get; }

        /// <summary>
        /// Gets AAD auth token for key vault call back.
        /// </summary>
        Task<string> GetAccessTokenAsync(string authority, string resource, string scope);
    }
}
