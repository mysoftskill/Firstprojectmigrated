using System.Threading.Tasks;
using Microsoft.PrivacyServices.PrivacyOperation.Client;
using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    /// <summary>
    /// Provides access to PXS client instance.
    /// </summary>
    public interface IPxsClientProvider
    {
        /// <summary>
        /// Gets instance of PXS client.
        /// </summary>
        IPrivacyOperationClient Instance { get; }

        /// <summary>
        /// Applies request context to PXS client operation.
        /// </summary>
        Task<T> ApplyRequestContext<T>(T operationArgs) where T : BasePrivacyOperationArgs;
    }
}
