
// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.MsaAgeOutFakeCommandWorker
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Interface for Account-Close-Queue-Manager
    /// </summary>
    public interface IMsaAgeOutQueue
    {
        /// <summary>
        ///     Gets a batch of messages from the queue.
        /// </summary>
        /// <param name="maxCount">max count of items to dequeue in batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A list of <see cref="AgeOutRequest" /></returns>
        Task<IList<IQueueItem<AgeOutRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken);
    }
}

