namespace Microsoft.Azure.ComplianceServices.Common.SecretClient
{
    /// <summary>
    /// A factory interface that produces key vault clients.
    /// </summary>
    public interface IAzureKeyVaultClientFactory
    {
        /// <summary>
        /// Creates a new <see cref="IAzureKeyVaultClient"/> that targets the given base URL.
        /// </summary>
        IAzureKeyVaultClient CreateKeyVaultClient(string baseUrl);

        /// <summary>
        /// Creates a new <see cref="IAzureKeyVaultClient"/> using default settings for this factory.
        /// </summary>
        IAzureKeyVaultClient CreateDefaultKeyVaultClient();
    }
}
