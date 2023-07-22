namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Collections.Generic;
    using SemanticComparison;
    using SemanticComparison.Fluent;

    /// <summary>
    /// Extensions that add likeness support to various types.
    /// </summary>
    public static class LikenessExtensions
    {
        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="T">The type for both sequences.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SequenceLike<T>(this IEnumerable<T> source, IEnumerable<T> target)
        {
            return SequenceLike(source, target, x => x.Likeness());
        }

        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SequenceLike<TSource, TTarget>(this IEnumerable<TSource> source, IEnumerable<TTarget> target)
        {
            return SequenceLike(source, target, x => x.Likeness<TSource, TTarget>());
        }

        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="getLikeness">A function to create a likeness from the source.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SequenceLike<TSource, TTarget>(this IEnumerable<TSource> source, IEnumerable<TTarget> target, Func<TSource, Likeness<TSource, TTarget>> getLikeness)
        {
            return source.SequenceAssert(target, (src, dest) => getLikeness(src).ShouldEqual(dest));
        }

        /// <summary>
        /// Add likeness support for comparing two sequences using a function to ensure proper order.
        /// </summary>
        /// <typeparam name="T">The test type.</typeparam>
        /// <typeparam name="TKey">The test sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderBy">Function to order both sequences.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SortedSequenceLike<T, TKey>(
            this IEnumerable<T> source,
            IEnumerable<T> target,
            Func<T, TKey> orderBy)
        {
            return SortedSequenceLike(source, target, orderBy, orderBy, x => x.Likeness());
        }

        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="T">The test type.</typeparam>
        /// <typeparam name="TKey">The test sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderBy">Function to order both sequences.</param>
        /// <param name="getLikeness">A function to create a likeness from the source.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SortedSequenceLike<T, TKey>(
            this IEnumerable<T> source,
            IEnumerable<T> target,
            Func<T, TKey> orderBy,
            Func<T, Likeness<T, T>> getLikeness)
        {
            return SortedSequenceLike(source, target, orderBy, orderBy, getLikeness);
        }

        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <typeparam name="TSourceKey">The source sort key type.</typeparam>
        /// <typeparam name="TTargetKey">The target sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderSourceBy">Function to order the source by.</param>
        /// <param name="orderTargetBy">Function to order the target by.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SortedSequenceLike<TSource, TTarget, TSourceKey, TTargetKey>(
            this IEnumerable<TSource> source,
            IEnumerable<TTarget> target,
            Func<TSource, TSourceKey> orderSourceBy,
            Func<TTarget, TTargetKey> orderTargetBy)
        {
            return SortedSequenceLike(source, target, orderSourceBy, orderTargetBy, x => x.Likeness<TSource, TTarget>());
        }

        /// <summary>
        /// Add likeness support for comparing two sequences.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <typeparam name="TSourceKey">The source sort key type.</typeparam>
        /// <typeparam name="TTargetKey">The target sort key type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="orderSourceBy">Function to order the source by.</param>
        /// <param name="orderTargetBy">Function to order the target by.</param>
        /// <param name="getLikeness">A function to create a likeness from the source.</param>
        /// <returns>True if every item in the sequence is equivalent using likeness.</returns>
        public static bool SortedSequenceLike<TSource, TTarget, TSourceKey, TTargetKey>(
            this IEnumerable<TSource> source, 
            IEnumerable<TTarget> target, 
            Func<TSource, TSourceKey> orderSourceBy,
            Func<TTarget, TTargetKey> orderTargetBy,
            Func<TSource, Likeness<TSource, TTarget>> getLikeness)
        {
            return source.SortedSequenceAssert(target, orderSourceBy, orderTargetBy, (src, dest) => getLikeness(src).ShouldEqual(dest));
        }

        /// <summary>
        /// Create a likeness from any value for it's own type.
        /// This is handy for basic comparisons.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>A likeness.</returns>
        public static Likeness<T, T> Likeness<T>(this T value)
        {
            return value.Likeness<T, T>();
        }

        /// <summary>
        /// Create a likeness from any value for any type.
        /// This is just a simple shortcut.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The value.</param>
        /// <returns>A likeness.</returns>
        public static Likeness<TSource, TTarget> Likeness<TSource, TTarget>(this TSource source)
        {
            return source.AsSource().OfLikeness<TTarget>();
        }

        /// <summary>
        /// Create a likeness from any value for any type and calls Equals.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <returns>A likeness.</returns>
        public static bool LikenessEquals<TSource, TTarget>(this TSource source, TTarget target)
        {
            return source.AsSource().OfLikeness<TTarget>().Equals(target);
        }

        /// <summary>
        /// Create a likeness from any value for any type and calls ShouldEqual.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <returns>True if the values are equal, otherwise throws an exception.</returns>
        public static bool LikenessShouldEqual<TSource, TTarget>(this TSource source, TTarget target)
        {
            source.AsSource().OfLikeness<TTarget>().ShouldEqual(target);
            return true;
        }

        /// <summary>
        /// Create a likeness from any value for any type and calls ShouldEqual.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="target">The target value.</param>
        /// <returns>True if the values are equal, otherwise throws an exception.</returns>
        public static bool ShouldEqual_<TSource, TTarget>(this Likeness<TSource, TTarget> source, TTarget target)
        {
            source.ShouldEqual(target);
            return true;
        }
    }
}