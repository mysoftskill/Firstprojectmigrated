// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;

    /// <summary>
    ///     IPrivacyOperationAuthClient
    /// </summary>
    public interface IPrivacyOperationAuthClient
    {
        /// <summary>
        ///     Get AAD access token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="userAssertion">The user assertion.</param>
        /// <returns>The acquired token.</returns>
        Task<AuthenticationHeaderValue> GetAadAuthToken(CancellationToken cancellationToken, UserAssertion userAssertion = null);
    }
}
