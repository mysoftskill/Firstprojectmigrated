using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.ServiceTreeClient
{
    /// <summary>
    /// Provides access to Service Tree client instance.
    /// </summary>
    public interface IServiceTreeClientProvider
    {
        /// <summary>
        /// Gets instance of Service Tree client.
        /// </summary>
        DataManagement.Client.ServiceTree.IServiceTreeClient Instance { get; }

        /// <summary>
        /// Creates new request context for Service Tree client operation.
        /// </summary>
        /// <returns></returns>
        Task<DataManagement.Client.RequestContext> CreateNewRequestContext();
    }
}
