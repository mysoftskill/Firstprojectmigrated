// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Contracts.Adapter.MsaTokenProvider
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Retrieves, caches, and refreshes MSA Identity Service tokens.
    /// </summary>
    public interface IMsaTokenProvider : IDisposable
    {
        /// <summary>
        /// Retrieves the current token, or retrieves a new token if the current one is expired.
        /// </summary>
        /// <param name="forceRefresh">True to request a new token even if caching is enabled. Any cached token would
        /// be replaced by the refreshed one. To be used in the scenario where a cached token is used, but reported to
        /// be invalid.</param>
        /// <returns>A response containing either a valid token or error information.</returns>
        Task<GetTokenResponse> GetTokenAsync(bool forceRefresh);
    }
}
