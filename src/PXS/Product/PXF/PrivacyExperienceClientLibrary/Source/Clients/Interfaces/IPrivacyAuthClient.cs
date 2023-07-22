// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Privacy Authentication-Client
    /// </summary>
    public interface IPrivacyAuthClient
    {
        /// <summary>
        /// Gets the X509 certificate used as the client side of the mutually authenticated SSL connection which will be established
        /// by this auth client
        /// </summary>
        /// <value>
        /// The HTTP client credential.
        /// </value>
        X509Certificate2 ClientCertificate { get; }
        
        /// <summary>
        /// Retrieve an access token asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the get token request.</param>
        /// <returns>A task that track the operations to fetch an auth token. 
        /// If successful, the auth token will be returned as a string.</returns>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    }
}