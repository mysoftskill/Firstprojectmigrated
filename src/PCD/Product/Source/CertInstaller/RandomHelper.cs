// -----------------------------------------------------------------------
// <copyright file="RandomHelper.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.
// </copyright>
// -----------------------------------------------------------------------
namespace CertInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// Defines thread safe functions for interacting with the Random class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class RandomHelper
    {
        private static ThreadLocal<Random> randomThreadLocal = new ThreadLocal<Random>(
            () => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));

        /// <summary>
        /// Gets a random value between 0 and 1.
        /// </summary>
        /// <returns>A random double.</returns>
        public static double NextDouble()
        {
            return randomThreadLocal.Value.NextDouble();
        }

        /// <summary>
        /// Creates a random integer.
        /// </summary>
        /// <returns>A random integer.</returns>
        public static int Next()
        {
            return randomThreadLocal.Value.Next();
        }

        /// <summary>
        /// Creates a random integer.
        /// </summary>
        /// <param name="minValue">The lower bound of the random value.</param>
        /// <param name="maxValue">The upper bound of the random value.</param>
        /// <returns>A random integer between the specified range.</returns>
        public static int Next(int minValue, int maxValue)
        {
            return randomThreadLocal.Value.Next(minValue, maxValue);
        }

        /// <summary>
        /// Takes a random element from the given list.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="items">A list of items.</param>
        /// <returns>A random pick for the input list.</returns>
        public static T TakeElement<T>(IList<T> items)
        {
            return items[RandomHelper.Next() % items.Count];
        }
    }
}
