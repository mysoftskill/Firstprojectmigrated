namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;

    public class TimeRangePredicateBuilder : TestDataBuilder<TimeRangePredicate>
    {
        protected override TimeRangePredicate CreateNewObject()
        {
            return new TimeRangePredicate
            {
                StartTime = DateTimeOffset.MinValue,
                EndTime = DateTimeOffset.UtcNow,
            };
        }
    }
}
