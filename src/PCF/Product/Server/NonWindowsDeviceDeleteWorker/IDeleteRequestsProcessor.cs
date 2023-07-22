// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// DeleteRequestsProcessor interface.
    /// </summary>
    public interface IDeleteRequestsProcessor
    {
        /// <summary>
        /// Process requests from json in given partition.
        /// </summary>
        /// <param name="partitionId">EventHub partition.</param>
        /// <param name="jsonDeleteRequest">1DS SDK delete request.</param>
        /// <returns></returns>
        void ProcessDeleteRequestsFromJson(string partitionId, string jsonDeleteRequest);

        /// <summary>
        /// Publish delete requests from partition.
        /// </summary>
        /// <param name="partitionId">EventHub partition id.</param>
        /// <returns></returns>
        Task PublishDeleteRequests(string partitionId);
    }
}
