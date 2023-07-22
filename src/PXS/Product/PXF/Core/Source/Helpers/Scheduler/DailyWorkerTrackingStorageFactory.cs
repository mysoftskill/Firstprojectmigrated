// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Factory class to create a DailyWorkerTrackingStorage instance
    /// We can not create CloudTableWrapper outside its assembly since it's an internal class.
    /// </summary>
    public static class DailyWorkerTrackingStorageFactory
    {
        public const string DeleteCoordinatorTableName = "deletecoordinator";
        public const string PrivacyWorkerTableName = "privacyworker";

        /// <summary>
        /// Static function to create a new DailyWorkerTrackingStorage
        /// </summary>
        /// <param name="cloudTable">The CloudTable instance used for the DailyWorkerTrackingStorage</param>
        /// <returns></returns>
        public static IDailyWorkerTrackingStorage CreateDailyWorkerTrackingStorage(ICloudTable cloudTable)
        {
            return new DailyWorkerTrackingStorage(cloudTable);
        }
    }
}
