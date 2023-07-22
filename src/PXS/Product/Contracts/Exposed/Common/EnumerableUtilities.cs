//--------------------------------------------------------------------------------
// <copyright file="EnumerableUtilities.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Contracts.Exposed
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// A helper class for enumerable collections. Same as Common\EnumerableUtilities but internal to ExposedContracts to avoid 
    /// external dependencies.
    /// </summary>
    internal static class EnumerableUtilities
    {
        #region non-generics

        /// <summary>
        /// This method is required because string.Join() does not have a non-generics version.
        /// </summary>
        /// <param name="enumerable">The enumerable to convert to a single string.</param>
        /// <returns>a generic "NameOfType[]" string</returns>
        public static string ToString(IEnumerable enumerable)
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            IEnumerator enumerator = enumerable.GetEnumerator();

            // If the enumeration has at least one element, then append it
            StringBuilder stringBuilder = new StringBuilder("[");
            if (enumerator.MoveNext())
            {
                stringBuilder.Append(enumerator.Current);

                // Now append any remaining elements delimited with a comma
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append(enumerator.Current);
                }
            }

            stringBuilder.Append("]");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// This method is required because string.Join() does not have a non-generics version.
        /// The extension method version of EnumerableUtilities.ToString()
        /// </summary>
        /// <param name="enumerable">The enumerable to convert to a single string.</param>
        /// <returns>a generic "NameOfType[]" string</returns>
        public static string ToJoinedString(this IEnumerable enumerable)
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            IEnumerator enumerator = enumerable.GetEnumerator();

            // If the enumeration has at least one element, then append it
            StringBuilder stringBuilder = new StringBuilder("[");
            if (enumerator.MoveNext())
            {
                stringBuilder.Append(enumerator.Current);

                // Now append any remaining elements delimited with a comma
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append(enumerator.Current);
                }
            }

            stringBuilder.Append("]");

            return stringBuilder.ToString();
        }

        #endregion

        #region generics

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate.</typeparam>
        /// <param name="enumerable">The enumerable list</param>
        /// <returns>A string whose value is the same as this instance.</returns>
        public static string ToString<T>(IEnumerable<T> enumerable)
        {
            return enumerable != null
                ? string.Format(CultureInfo.InvariantCulture, "[{0}]", string.Join(", ", enumerable))
                : string.Empty;
        }

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// The extension method version of EnumerableUtilities.ToString().
        /// Named "ToJoinedString()" because extension methods do not take priority and cannot override instance methods.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate.</typeparam>
        /// <param name="values">A collection that contains the objects to concatenate.</param>
        /// <returns>A string whose value is the same as this instance.</returns>
        /// <param name="separator">The string to use as a separator.</param>
        /// <returns>A string that consists of the members of values delimited by the <paramref name="separator"/> string.</returns>
        public static string ToJoinedString<T>(this IEnumerable<T> values, string separator = ", ")
        {
            string result = values != null
                ? string.Format(CultureInfo.InvariantCulture, "[{0}]", string.Join(separator, values))
                : string.Empty;

            return result;
        }

        /// <summary>
        /// Determines if the given sequence is sorted in descending order according to a key.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The sequence to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>true if the sequence is sorted; otherwise false.</returns>
        public static bool IsOrderedByDescending<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }

            Comparer<TKey> comparer = Comparer<TKey>.Default;
            using (IEnumerator<T> iterator = source.GetEnumerator())
            {
                // Sorted if list is empty
                if (!iterator.MoveNext())
                {
                    return true;
                }

                TKey current = keySelector(iterator.Current);

                while (iterator.MoveNext())
                {
                    TKey next = keySelector(iterator.Current);
                    if (comparer.Compare(current, next) < 0)
                    {
                        return false;
                    }

                    current = next;
                }
            }

            return true;
        }

        #endregion
    }
}
