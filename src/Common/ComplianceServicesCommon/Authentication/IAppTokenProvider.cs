// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System.Threading.Tasks;

    /// <summary>
    ///     ITokenManager
    /// </summary>
    public interface IAppTokenProvider
    {
        /// <summary>
        ///     Gets the app token async.
        /// </summary>
        /// <returns></returns>
        Task<string> GetAppTokenAsync();
    }
}
