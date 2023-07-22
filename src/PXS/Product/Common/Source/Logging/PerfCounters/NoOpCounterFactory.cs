// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    public class NoOpCounterFactory : ICounterFactory
    {
        public ICounter GetCounter(string categoryName, string counterName, CounterType counterType)
        {
            return new NoOpCounter();
        }
    }
}
