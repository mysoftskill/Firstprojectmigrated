namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// IDistributedLockBlobPrimitivesFactory
    /// </summary>
    public interface IDistributedLockBlobPrimitivesFactory
    {
        /// <summary>
        ///  Create an instance of AzureBloblockPrimitives
        /// </summary>
        /// <param name="distributedLockConfig"></param>
        /// <param name="blobName"></param>
        /// <param name="uamiId"></param>
        /// <returns></returns>
        IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> CreateBloblockPrimitives(
            IDistributedLockConfiguration distributedLockConfig, 
            string blobName, 
            string uamiId);
    }
}
