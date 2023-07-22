namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// Aggregate count of all resources requested
    /// </summary>
    public class AggregateCountResources
    {
        /// <summary>
        /// Count of items in ResourceCollection
        /// </summary>
        public Dictionary<ResourceType, int> resourceCount;
    }
}
