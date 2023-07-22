// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public interface IAssetGroupAzureQueueTrackerCache
    {
        /// <summary>
        /// Returns a bool indicating if the queue exists associated with the given params
        /// </summary>
        /// <param name="cloudQueue">The queue to track</param>
        /// <returns>Bool indicating if the queue exists or not</returns>
        bool QueueExists(IAzureCloudQueue cloudQueue);

        /// <summary>
        /// Starts the queue tracker for the given params
        /// </summary>
        /// <param name="cloudQueue">The queue to track</param>
        /// <param name="commandType">The command type</param>
        void StartQueueTracker(IAzureCloudQueue cloudQueue, PrivacyCommandType commandType);
    }
}
