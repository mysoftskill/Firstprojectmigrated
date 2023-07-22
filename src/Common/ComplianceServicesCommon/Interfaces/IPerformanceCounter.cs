namespace Microsoft.Azure.ComplianceServices.Common.Interfaces
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interface for performance counters so we can hide the implementation.
    /// </summary>
    [ComVisible(false)]
    public interface IPerformanceCounter
    {
        /// <summary>
        /// Increments the counter.
        /// </summary>
        void Increment(int incrementBy = 1);

        /// <summary>
        /// Increments the counter instance.
        /// </summary>
        void Increment(string instance, int incrementBy = 1);

        /// <summary>
        /// Decrements the counter.
        /// </summary>
        void Decrement(int decrementBy = 1);

        /// <summary>
        /// Decrements the counter instance.
        /// </summary>
        void Decrement(string instance, int decrementBy = 1);

        /// <summary>
        /// Sets the value of the counter. Useful for things like latency, etc.
        /// </summary>
        void Set(int value);

        /// <summary>
        /// Sets the value of the counter. Useful for things like latency, etc.
        /// </summary>
        void Set(string instance, int value);

        /// <summary>
        /// Gets the value of the counter.
        /// </summary>
        ulong GetValue();
    }
}
