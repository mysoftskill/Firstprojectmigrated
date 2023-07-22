namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     IRecurrentDeleteWorkerFactory
    /// </summary>
    public interface IRecurrentDeleteWorkerFactory
    {
        /// <summary>
        ///     Creates a new instance of RecurrentDeleteWorker />
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <returns></returns>
        IWorker CreateRecurrentDeleteScheduleWorker(IAzureStorageConfiguration queueConfig);

        /// <summary>
        ///     Creates a new instance of RecurrentDeleteScheduleScanner/>
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <returns></returns>
        IWorker CreateRecurrentDeleteScheduleScanner(IAzureStorageConfiguration queueConfig);

        /// <summary>
        ///     Creates a new instance of RecurrentDeleteWorker />
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <returns></returns>
        IWorker CreatePreVerifierScanner(IAzureStorageConfiguration queueConfig);

        /// <summary>
        ///     Creates a new instance of RecurrentDeleteWorker />
        /// </summary>
        /// <param name="queueConfig"></param>
        /// <returns></returns>
        IWorker CreatePreVerifierWorker(IAzureStorageConfiguration queueConfig);
    }
}
