// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;

    /// <summary>
    ///     Factory class to create a WorkerTrackingStorage instance
    ///     We can not create CloudTableWrapper outside its assembly since it's an internal class.
    /// </summary>
    public static class WorkerTrackingStorageFactory
    {
        /// <summary>
        ///     Static function to create a new DailyWorkerTrackingStorage
        /// </summary>
        /// <param name="cloudTable">The CloudTable instance used for the DailyWorkerTrackingStorage</param>
        /// <returns></returns>
        public static IWorkerTrackingStorage CreateWorkerTrackingStorage(ICloudTable cloudTable)
        {
            return new WorkerTrackingStorage(cloudTable);
        }
    }
}
