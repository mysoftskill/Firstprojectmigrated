// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Privacy.Core.Throttling
{
    /// <summary>
    ///     An interface for generally throttling requests by some opaque key.
    /// </summary>
    public interface IRequestThrottler
    {
        /// <summary>
        ///     Returns true or false for if a request indicated by some opaque key should be throttled.
        /// </summary>
        /// <param name="key">The opaque key, such as a tenant, or a user, or some axis that is throttled.</param>
        /// <returns>True if the request should be throttled, otherwise false and increments the hit count.</returns>
        bool ShouldThrottle(string key);

        /// <summary>
        ///     Returns true or false for if a request indicated by some opaque key should be throttled.
        /// </summary>
        /// <param name="key">The opaque key, such as a tenant, or a user, or some axis that is throttled.</param>
        /// <param name="cToken">The cancellation token</param>
        /// <returns>True if the request should be throttled, otherwise false and increments the hit count.</returns>
        Task<bool> ShouldThrottleAsync(string key, CancellationToken cToken = default(CancellationToken));
    }
}
