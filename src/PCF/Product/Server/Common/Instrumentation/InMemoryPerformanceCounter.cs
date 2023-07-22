namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Threading;

    using Microsoft.Azure.ComplianceServices.Common.Interfaces;

    /// <summary>
    /// A mock perf counter class for use when APSDK can't be used. Should only be used for dev box runs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class InMemoryPerformanceCounter : IPerformanceCounter
    {
        private long currentValue = 0;
        private readonly string category;
        private readonly string name;

        public InMemoryPerformanceCounter(string category, string name)
        {
            this.category = category;
            this.name = name;
        }

        public void Increment(int incrementBy)
        {
            Interlocked.Add(ref this.currentValue, incrementBy);
        }

        /// <summary>
        /// Increments the counter instance.
        /// </summary>
        public void Increment(string instance, int incrementBy = 1)
        {
            this.Increment(incrementBy);
        }

        public void Decrement(int decrementBy)
        {
            this.Increment(-decrementBy);
        }

        /// <summary>
        /// Decrements the counter instance.
        /// </summary>
        public void Decrement(string instance, int decrementBy = 1)
        {
            this.Decrement(decrementBy);
        }

        public void Set(int value)
        {
            Interlocked.Exchange(ref this.currentValue, value);
        }

        public void Set(string instance, int value)
        {
            this.Set(value);
        }

        public ulong GetValue()
        {
            return (ulong)this.currentValue;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "\\{0}\\{1} = {2}", this.category, this.name, this.currentValue);
        }
    }
}
