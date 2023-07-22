
using System;

namespace Microsoft.Azure.ComplianceServices.Common
{
    /// <summary>
    /// Database id pass to ConnectionMultiplexer.GetDatabase api
    /// Different scenarios should use different database to avoid key collision.
    /// </summary>
    public enum RedisDatabaseId
    {
        Default = -1,

        [Obsolete("Used before for vortex request deduping. Still contain related data.")]
        UsedForIncomingVortexDedupBeforeDiscardedNow = 1,

        [Obsolete("Used before for vortex request deduping. Still contain related data.")]
        UsedForOutgoingVortexDedupBeforeDiscardedNow = 2,

        VortexNonSystemRequestDedup = 3,

        VortexSystemRequestDedup = 4,

        CosmosDbPartitionSizeWorkerData = 5,

        AnaheimIdRequestDedup = 6
    }
}
