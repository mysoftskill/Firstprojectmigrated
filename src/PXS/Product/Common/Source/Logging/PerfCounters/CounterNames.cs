//--------------------------------------------------------------------------------
// <copyright file="CounterNames.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    /// <summary>
    /// Counter names.
    /// </summary>
    internal static class CounterNames
    {
        /// <summary>
        /// Request counter-name.
        /// </summary>
        public const string Requests = "Requests";

        /// <summary>
        /// Requests/second counter-name.
        /// </summary>
        public const string RequestsPerSecond = "Requests per Second";

        /// <summary>
        /// Errors/seconds counter-name.
        /// </summary>
        public const string ErrorsPerSecond = "Errors per Second";

        /// <summary>
        /// Http4** errors/second counter-name.
        /// </summary>
        public const string Error4XXPerSecond = "4xx per Second";

        /// <summary>
        /// Http5** errors/second counter-name.
        /// </summary>
        public const string Error5XXPerSecond = "5xx per Second";

        /// <summary>
        /// Latency/second counter-name.
        /// </summary>
        public const string Latency = "Latency";

        /// <summary>
        /// Percentile-latency/second counter-name.
        /// </summary>
        public const string PercentileLatency = "PercentileLatency";

        /// <summary>
        /// Connection count counter-name.
        /// </summary>
        public const string ConnectionCount = "ConnectionCount";
    }
}
