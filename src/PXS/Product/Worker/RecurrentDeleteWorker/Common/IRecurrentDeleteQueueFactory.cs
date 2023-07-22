namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;

    /// <summary>
    /// IRecurrentDeleteQueueFactory
    /// </summary>
    public interface IRecurrentDeleteQueueFactory
    {
        /// <summary>
        ///     Creates a new instance of AzureCloudQueue />
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <param name="uami"></param>
        /// <returns></returns>
        ICloudQueue<RecurrentDeleteScheduleDbDocument> CreateScheduleQueue(IAzureStorageConfiguration queueConfig, string uami);

        /// <summary>
        ///     Creates a new instance of AzureCloudQueue />
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <param name="uami"></param>
        /// <returns></returns>
        ICloudQueue<RecurrentDeleteScheduleDbDocument> CreatePreVerifierQueue(IAzureStorageConfiguration queueConfig, string uami);
    }
}
