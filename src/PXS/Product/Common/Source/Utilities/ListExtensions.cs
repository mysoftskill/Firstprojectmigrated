// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Collections.Generic;
    using System.Linq;

    public static class ListExtensions
    {
        /// <summary>
        /// Returns a string that represents the current list with all members of the collection printed
        /// using the default ToString() for their type.
        /// </summary>
        /// <typeparam name="T">The type of elements of the input sequence.</typeparam>
        /// <param name="collection">The sequence to represent as a string.</param>
        /// <returns>A string representation of the sequence.</returns>
        public static string ToStringComplete<T>(this IList<T> collection)
        {
            // Follows JSON formatting of arrays, comma separated list enclosed in square brackets
            return "[{0}]".FormatInvariant(string.Join(", ", collection));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements by using the default
        /// quality comparer for their type. This method is safe and Will not throw if first or second is null.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An IList to compare to second."/></param>
        /// <param name="second">An IList to compare to the first sequence.</param>
        /// <returns><see langword="true"/> if the two source sequences are of equal length and their corresponding elements are
        /// equal according to the default equality comparer for their type; otherwise, <see langword="false"/></returns>
        public static bool SafeSequentialEquals<T>(this IList<T> first, IList<T> second)
        {
            return (first == null)
                ? second == null
                : first.SequenceEqual(second);
        }
    }
}
