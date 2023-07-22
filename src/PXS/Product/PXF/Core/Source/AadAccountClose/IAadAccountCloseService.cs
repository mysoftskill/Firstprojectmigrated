// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    /// </summary>
    public interface IAadAccountCloseService
    {
        /// <summary>
        ///     Post a batch of account close requests to PCF.
        /// </summary>
        /// <param name="batch">The batch of account close requests to post.</param>
        Task<IList<ServiceResponse<IQueueItem<AccountCloseRequest>>>> PostBatchAccountCloseAsync(
            IList<IQueueItem<AccountCloseRequest>> batch);
    }
}
