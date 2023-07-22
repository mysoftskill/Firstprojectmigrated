// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    /// <summary>
    ///     This interface enables creation or retrieval of counters.
    /// </summary>
    public interface ICounterFactory
    {
        /// <summary>
        ///     Retrieves the instance of <seealso cref="ICounter" /> associated with the specified counter information.
        /// </summary>
        /// <param name="categoryName">The counter category name.</param>
        /// <param name="counterName">The counter name.</param>
        /// <param name="counterType">The counter type.</param>
        /// <returns>The instance of <seealso cref="ICounter" /> associated with the specified counter information</returns>
        ICounter GetCounter(string categoryName, string counterName, CounterType counterType);
    }
}
