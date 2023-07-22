// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     ITokenManager
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        ///     Gets the app token async.
        /// </summary>
        /// <param name="authority">The token authority</param>
        /// <param name="clientId">The client id</param>
        /// <param name="resource">The resource</param>
        /// <param name="certificate">The certificate</param>
        /// <param name="cacheable">a boolean that determines if the confidentialCredential client will be cached</param>
        /// <param name="logger">The logger</param>
        /// <returns>The app token</returns>
        Task<string> GetAppTokenAsync(string authority, string clientId, string resource, X509Certificate2 certificate, bool cacheable = true, ILogger logger = null);
    }
}
