// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    /// <summary>
    ///     Interface for Privacy Operation Client.
    /// </summary>
    public interface IPrivacyOperationClient
    {
        /// <summary>
        ///     Get the list of submitted operations for a specific caller.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>A list response.</returns>
        Task<IList<PrivacyRequestStatus>> ListRequestsAsync(ListOperationArgs request);

        /// <summary>
        ///     Post a delete operation.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>A delete response.</returns>
        Task<DeleteOperationResponse> PostDeleteRequestAsync(DeleteOperationArgs request);

        /// <summary>
        ///     Post an export operation.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>An export response.</returns>
        Task<ExportOperationResponse> PostExportRequestAsync(ExportOperationArgs request);
    }
}
