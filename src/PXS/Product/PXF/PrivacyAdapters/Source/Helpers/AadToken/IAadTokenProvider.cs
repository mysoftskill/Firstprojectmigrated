// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for classes that provide the ability to generate AAD token
    /// </summary>
    public interface IAadTokenProvider
    {
        /// <summary>
        ///     Generates a PoP authenticator with an AAD token  for accessing the specified resource
        /// </summary>
        /// <param name="request">data to make PoP authenticator AAD request</param>
        /// <param name="cancelToken">cancellation token to halt the request</param>
        /// <returns>requested authenticator</returns>
        Task<string> GetPopTokenAsync(
            AadPopTokenRequest request,
            CancellationToken cancelToken);
    }
}
