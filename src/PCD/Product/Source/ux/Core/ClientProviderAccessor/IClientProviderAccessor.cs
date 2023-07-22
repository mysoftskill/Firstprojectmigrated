namespace Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor
{
    /// <summary>
    /// Provides access to client provider with either the real client or mocked client.
    /// </summary>
    public interface IClientProviderAccessor<TClientProvider>
    {
        TClientProvider ProviderInstance { get; }
    }
}
