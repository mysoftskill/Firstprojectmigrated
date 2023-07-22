// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     Interface for handling Vortex events
    /// </summary>
    public interface IVortexEventService
    {
        /// <summary>
        ///     Handles device delete requests in batch
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns>A collection of service responses indicating success or failure of each batch item</returns>
        Task<IEnumerable<ServiceResponse<IQueueItem<DeviceDeleteRequest>>>> DeleteDevicesAsync(IEnumerable<IQueueItem<DeviceDeleteRequest>> requests);

        /// <summary>
        ///     Queues the valid events to be processed by worker
        /// </summary>
        /// <param name="vortexEventJson">The vortex event json.</param>
        /// <param name="info">The request information.</param>
        /// <returns>A service response indicating success or failure</returns>
        Task<ServiceResponse> QueueValidEventsAsync(byte[] vortexEventJson, VortexRequestInformation info);

        /// <summary>
        ///     Converts DeviceDeleteRequest to DeviceDeleteId request and Sends to eventhub for Anaheim team's use
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<bool> SendAnaheimDeviceDeleteIdRequestAsync(DeviceDeleteRequest request);
    }
}
