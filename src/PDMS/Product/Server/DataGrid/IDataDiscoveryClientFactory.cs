namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System.Threading.Tasks;

    using Microsoft.DataPlatform.DataDiscovery;

    /// <summary>
    /// Defines methods for creating discovery clients.
    /// </summary>
    public interface IDataDiscoveryClientFactory
    {
        /// <summary>
        /// Creates a data discovery client for the current authenticated user or app id, depending on resource Id.
        /// </summary>
        /// <returns>The client.</returns>
        Task<IDataDiscoveryClient> CreateClientAsync();
    }
}