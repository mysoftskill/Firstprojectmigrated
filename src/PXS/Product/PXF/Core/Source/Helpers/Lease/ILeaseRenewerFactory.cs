// ---------------------------------------------------------------------------
// <copyright file="ILeaseRenewerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Lease
{
    /// <summary>
    ///     contract for classes that can create lease renewer objects
    /// </summary>
    public interface ILeaseRenewerFactory
    {
        /// <summary>
        ///     Creates a new lease renewer
        /// </summary>
        /// <returns>resulting value</returns>
        ILeaseRenewer Create();
    }
}
