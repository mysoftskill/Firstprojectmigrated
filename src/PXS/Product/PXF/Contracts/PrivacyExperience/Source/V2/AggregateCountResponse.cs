namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     The aggregate count response for TimelineAggregateCount API
    /// </summary>
    public class AggregateCountResponse
    {
        /// <summary>
        ///     Dictionary of the cardType(string) and the count
        /// </summary>
        public Dictionary<string, int> AggregateCounts { get; set; }
    }
}
