namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Configuration;

    public interface IAnaheimIdQueueFactory
    {
        ICloudQueue<AnaheimIdRequest> Create(IAzureStorageConfiguration queueConfig);
    }
}
