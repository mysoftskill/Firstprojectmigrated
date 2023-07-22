// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Interface for Account-Close-Queue-Manager
    /// </summary>
    public interface IAccountCloseQueueManager
    {
        /// <summary>
        ///     Enqueues the <see cref="List{AccountCloseRequest}" />
        /// </summary>
        /// <param name="accountCloseRequests">The account close request batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task EnqueueAsync(IEnumerable<AccountCloseRequest> accountCloseRequests, CancellationToken cancellationToken);

        /// <summary>
        ///     Enqueues the <see cref="List{AccountCloseRequest}" />
        /// </summary>
        /// <param name="accountCloseRequests">The account close request batch</param>
        /// <param name="invisibilityTimer">Time for which message remains invisible</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task EnqueueAsync(IEnumerable<AccountCloseRequest> accountCloseRequests, TimeSpan invisibilityTimer, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a batch of messages from the queue.
        /// </summary>
        /// <param name="maxCount">max count of items to dequeue in batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A list of <see cref="AccountCloseRequest" /></returns>
        Task<IList<IQueueItem<AccountCloseRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken);
    }
}
