namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// IAnaheimIdQueueWorkerFactory
    /// </summary>
    public interface IAnaheimIdQueueWorkerFactory
    {
        IWorker Create(IAzureStorageConfiguration queueConfig);
    }
}
