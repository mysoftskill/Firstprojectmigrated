using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Provides access to PDMS client instance.
    /// </summary>
    public interface IPdmsClientProvider
    {
        /// <summary>
        /// Gets instance of PDMS client.
        /// </summary>
        DataManagement.Client.V2.IDataManagementClient Instance { get; }

        /// <summary>
        /// Creates new request context for PDMS client operation.
        /// </summary>
        Task<RequestContext> CreateNewRequestContext();

        /// <summary>
        /// Creates new request context for PDMS client operation, based on passed in JWT Auth token.
        /// </summary>
        RequestContext CreateNewRequestContext(string jwtAuthToken);
    }
}
