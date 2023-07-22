namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Extends assert functionality.
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Compares two sequences. Ensures both have the same count and order.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="assertEqual">A function to compare source and target using assertions.</param>
        /// <returns>True if every item in the sequence is passes or else an exception is thrown.</returns>
        public static bool SequenceAssert<TSource, TTarget>(this IEnumerable<TSource> source, IEnumerable<TTarget> target, Action<TSource, TTarget> assertEqual)
        {
            if (source == null || target == null)
            {
                Assert.Null(source);
                Assert.Null(target);
            }
            else
            {
                Assert.Equal(source.Count(), target.Count());

                for (var i = 0; i < source.Count(); i++)
                {
                    assertEqual(source.ElementAt(i), target.ElementAt(i));
                }
            }

            return true;
        }

        /// <summary>
        /// Compares two sequences. Ensures both have the same count and order.
        /// </summary>
        /// <typeparam name="T">The source type.</typeparam>
        /// <typeparam name="TKey">The source sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderBy">Function to order the sequences.</param>
        /// <param name="assertEqual">A function to compare source and target using assertions.</param>
        /// <returns>True if every item in the sequence is passes or else an exception is thrown.</returns>
        public static bool SortedSequenceAssert<T, TKey>(
            this IEnumerable<T> source,
            IEnumerable<T> target,
            Func<T, TKey> orderBy,
            Action<T, T> assertEqual)
        {
            return SortedSequenceAssert(source, target, orderBy, orderBy, assertEqual);
        }

        /// <summary>
        /// Compares two sequences. Ensures both have the same count and order.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <typeparam name="TSourceKey">The source sort key type.</typeparam>
        /// <typeparam name="TTargetKey">The target sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderSourceBy">Function to order the source by.</param>
        /// <param name="orderTargetBy">Function to order the target by.</param>
        /// <param name="assertEqual">A function to compare source and target using assertions.</param>
        /// <returns>True if every item in the sequence is passes or else an exception is thrown.</returns>
        public static bool SortedSequenceAssert<TSource, TTarget, TSourceKey, TTargetKey>(
            this IEnumerable<TSource> source,
            IEnumerable<TTarget> target,
            Func<TSource, TSourceKey> orderSourceBy,
            Func<TTarget, TTargetKey> orderTargetBy,
            Action<TSource, TTarget> assertEqual)
        {
            if (source == null || target == null)
            {
                Assert.Null(source);
                Assert.Null(target);
            }
            else
            {
                source.OrderBy(orderSourceBy).SequenceAssert(target.OrderBy(orderTargetBy), assertEqual);
            }

            return true;
        }
    }
}