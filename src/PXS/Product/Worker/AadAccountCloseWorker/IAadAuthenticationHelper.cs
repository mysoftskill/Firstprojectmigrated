// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker
{
    using System.Threading.Tasks;

    /// <summary>
    ///     Helper for getting Aad Access Token
    /// </summary>
    public interface IAadAuthenticationHelper
    {
        /// <summary>
        ///     Gets the Aad Access Token using the configured Aad App Id and Certificate
        /// </summary>
        /// <returns>The Aad Access Token</returns>
        Task<string> GetAccessTokenAsync(string authority, string resource, string scope);
    }
}
