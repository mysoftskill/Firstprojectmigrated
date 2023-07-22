namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Defines thread safe functions for interacting with the Random class.
    /// </summary>
    public static class RandomHelper
    {
        private static readonly ThreadLocal<Random> randomThreadLocal = new ThreadLocal<Random>(
            () => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));

        /// <summary>
        /// Gets a random value between 0 and 1.
        /// </summary>
        public static double NextDouble()
        {
            return randomThreadLocal.Value.NextDouble();
        }

        /// <summary>
        /// Creates a random integer.
        /// </summary>
        public static int Next()
        {
            return randomThreadLocal.Value.Next();
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">
        /// The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number returned. 
        /// maxValue must be greater than or equal to minValue.
        /// </param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        /// that is, the range of return values includes minValue but not maxValue. If minValue
        /// equals maxValue, minValue is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// minValue is greater than maxValue
        /// </exception>
        public static int Next(int minValue, int maxValue)
        {
            return randomThreadLocal.Value.Next(minValue, maxValue);
        }
        
        /// <summary>
        /// Takes a random element from the given list.
        /// </summary>
        public static T TakeElement<T>(IList<T> items)
        {
            return items[RandomHelper.Next() % items.Count];
        }
    }
}
