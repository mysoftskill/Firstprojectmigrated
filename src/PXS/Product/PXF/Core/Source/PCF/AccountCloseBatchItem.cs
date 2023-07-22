// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.PCF
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    public class AccountCloseBatchItem
    {
        public ServiceResponse<IQueueItem<AccountCloseRequest>> QueueItem { get; }

        public Task<ServiceResponse> VerifierTask { get; }

        /// <summary>
        /// Creates an <see cref="AccountCloseBatchItem"/>
        /// </summary>
        /// <param name="queueItem"></param>
        /// <param name="verifierTask"></param>
        public AccountCloseBatchItem(ServiceResponse<IQueueItem<AccountCloseRequest>> queueItem, Task<ServiceResponse> verifierTask)
        {
            this.QueueItem = queueItem;
            this.VerifierTask = verifierTask;
        }
    }
}
