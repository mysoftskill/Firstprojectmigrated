//--------------------------------------------------------------------------------
// <copyright file="ICounter.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    /// <summary>
    /// This interface defines the methods for a counter.
    /// </summary>
    public interface ICounter
    {
        /// <summary>
        /// Sets the value of the default instance counter.
        /// </summary>
        /// <param name="counterValue">The value to set the counter to.</param>
        void SetValue(ulong counterValue);

        /// <summary>
        /// Sets the value of the specified instance counter.
        /// </summary>
        /// <param name="counterValue">The value to set the counter to..</param>
        /// <param name="instanceName">The instance counter to affect.</param>
        void SetValue(ulong counterValue, string instanceName);

        /// <summary>
        /// Increment the default instance counter.
        /// </summary>
        void Increment();

        /// <summary>
        /// Increment the specified instance counter.
        /// </summary>
        /// <param name="instanceName">The instance counter to affect.</param>
        void Increment(string instanceName);

        /// <summary>
        /// Increment the default instance counter by the specific amount.
        /// </summary>
        /// <param name="counterValue">The amount to increment by.</param>
        void IncrementBy(ulong counterValue);

        /// <summary>
        /// Increment the specified instance counter by the specific amount.
        /// </summary>
        /// <param name="counterValue">The amount to increment by.</param>
        /// <param name="instanceName">The instance counter to affect.</param>
        void IncrementBy(ulong counterValue, string instanceName);

        /// <summary>
        /// Decrement the default instance counter.
        /// </summary>
        void Decrement();

        /// <summary>
        /// Decrement the specified instance counter.
        /// </summary>
        /// <param name="instanceName">The instance counter to affect.</param>
        void Decrement(string instanceName);

        /// <summary>
        /// Decrement the default instance counter by the specific amount.
        /// </summary>
        /// <param name="counterValue">The amount to Decrement by.</param>
        void DecrementBy(ulong counterValue);

        /// <summary>
        /// Decrement the specified instance counter by the specific amount.
        /// </summary>
        /// <param name="counterValue">The amount to Decrement by.</param>
        /// <param name="instanceName">The instance counter to affect.</param>
        void DecrementBy(ulong counterValue, string instanceName);

        /// <summary>
        /// Get the value of the default instance counter.
        /// </summary>
        /// <returns>The value of the counter.</returns>
        ulong GetValue();

        /// <summary>
        /// Get the value of the specified instance counter.
        /// </summary>
        /// <param name="instanceName">The instance counter to affect.</param>
        /// <returns>The value of the counter.</returns>
        ulong GetValue(string instanceName);
    }
}
