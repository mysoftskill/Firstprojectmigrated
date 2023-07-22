//--------------------------------------------------------------------------------
// <copyright file="CounterType.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    /// <summary>
    /// Enumeration type for different counter types.
    /// </summary>
    public enum CounterType
    {
        /// <summary>
        /// None type.
        /// </summary>
        None,

        /// <summary>
        /// Number type.
        /// <para>
        /// Measures the latency of a transaction or the size of a blob of data. In the configuration file perfcounters.xml, 
        /// you can declare whether to report the min, max, average, sum or simply the last of the values set over an interval. 
        /// https://sharepoint/sites/autopilot/wiki/Perf%20Counters.aspx
        /// </para>
        /// </summary>
        Number,

        /// <summary>
        /// NumberPercentile type.
        /// <para>
        /// Measures the value below which a given percentage of the last M values lie.
        /// https://sharepoint/sites/autopilot/wiki/Perf%20Counters/Percentile%20Counters.aspx
        /// </para>
        /// </summary>
        NumberPercentile,

        /// <summary>
        /// Rate type.
        /// <para>
        /// Measures either the number of transactions handled or the arrival rate of those transactions.
        /// As the flag name implies, the value reported for a sequence of updates is the rate at which the
        /// counter's value is changing; the units for the value are transactions arrived/second. 
        /// In addition, in the configuration file perfcounters.xml, you can specify to report the last absolute value,
        /// so that a single perf counter can report both current rate and cumulative total (since last restart).
        /// https://sharepoint/sites/autopilot/wiki/Perf%20Counters.aspx
        /// </para>
        /// </summary>
        Rate,
    }
}
