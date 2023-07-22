// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    /// <summary>
    ///     Interface for VortexDeviceDeleteQueueManager
    /// </summary>
    public interface IVortexDeviceDeleteQueueManager
    {
        /// <summary>
        ///     Gets a batch of messages from the queue.
        /// </summary>
        /// <param name="maxCount">max count of items to dequeue in batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A list of <see cref="DeviceDeleteRequest" /></returns>
        Task<IList<IQueueItem<DeviceDeleteRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues delete requests
        /// </summary>
        /// <param name="deviceDeleteRequests">requests to be enqueued</param>
        /// <param name="cancellationToken"></param>
        Task EnqueueAsync(IEnumerable<DeviceDeleteRequest> deviceDeleteRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Enques a delete request
        /// </summary>
        /// <param name="deviceDeleteRequest">The delete request</param>
        /// <param name="cancellationToken"></param>
        Task EnqueueAsync(DeviceDeleteRequest deviceDeleteRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues an event with delayed visibility
        /// </summary>
        /// <param name="deviceDeleteRequests">Delete Requests</param>
        /// <param name="invisibilityDelay">Time until it's visible in the queue</param>
        /// <param name="cancellationToken"></param>
        Task EnqueueAsync(IEnumerable<DeviceDeleteRequest> deviceDeleteRequests, TimeSpan? invisibilityDelay, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues a device delete request
        /// </summary>
        /// <param name="deviceDeleteRequest"></param>
        /// <param name="invisibilityDelay"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task EnqueueAsync(DeviceDeleteRequest deviceDeleteRequest, TimeSpan? invisibilityDelay, CancellationToken cancellationToken);
    }
}
