// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId
{
    using System.Threading.Tasks;

    public interface IDistributedId
    {
        /// <summary>
        ///     Gets the assigned id
        /// </summary>
        long Id { get; }

        /// <summary>
        ///     Releases the id
        /// </summary>
        Task ReleaseAsync();

        /// <summary>
        ///     Renews the id length
        /// </summary>
        Task RenewAsync();
    }
}
