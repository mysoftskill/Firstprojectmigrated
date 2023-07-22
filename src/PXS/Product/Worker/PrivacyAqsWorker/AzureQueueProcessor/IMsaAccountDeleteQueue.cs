// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    /// <summary>
    ///     Interface for Msa AccountDeleteQueue
    /// </summary>
    public interface IMsaAccountDeleteQueue
    {
        /// <summary>
        ///     Enqueue the <see cref="IEnumerable{AccountDeleteInformation}" />
        /// </summary>
        /// <param name="accountCloseRequests">The account close request batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task EnqueueAsync(IEnumerable<AccountDeleteInformation> accountCloseRequests, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a batch of messages from the queue.
        /// </summary>
        /// <param name="maxCount">max count of items to dequeue in batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A list of <see cref="IQueueItem{AccountDeleteInformation}"/></returns>
        Task<IList<IQueueItem<AccountDeleteInformation>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken);
    }
}
