namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory
{
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;

    /// <summary>
    /// Decorates the base active directory for caching. Adds a property to indicate whether force refreshing is needed.
    /// </summary>
    public interface ICachedActiveDirectory : IActiveDirectory
    {
        /// <summary>
        /// Gets or sets a value indicating whether refreshing the cache is always needed.
        /// </summary>
        bool ForceRefreshCache { get; set; }
    }
}