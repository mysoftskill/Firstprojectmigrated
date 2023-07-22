// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId
{
    using System;
    using System.Threading.Tasks;

    public interface IDistributedIdFactory
    {
        /// <summary>
        ///     Gets an id to use for a given amount of time
        /// </summary>
        /// <param name="leaseTime">the desired time to have the id</param>
        /// <returns>an id</returns>
        Task<IDistributedId> AcquireIdAsync(TimeSpan leaseTime);
    }
}
