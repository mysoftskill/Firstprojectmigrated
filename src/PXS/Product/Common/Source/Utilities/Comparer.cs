// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;

    /// <summary>
    ///     contains various utility methods for comparing objects
    /// </summary>
    public static class Comparer
    {
        /// <summary>
        ///     Returns the maximum of two objects
        /// </summary>
        /// <typeparam name="T">type of object to test</typeparam>
        /// <param name="obj1">first object to test</param>
        /// <param name="obj2">second object to test</param>
        /// <returns>resulting value</returns>
        public static T Max<T>(
            T obj1,
            T obj2)
            where T : IComparable<T>
        {
            return obj1.CompareTo(obj2) > 0 ? obj1 : obj2;
        }

        /// <summary>
        ///     Returns the minimum of two objects
        /// </summary>
        /// <typeparam name="T">type of object to test</typeparam>
        /// <param name="obj1">first object to test</param>
        /// <param name="obj2">second object to test</param>
        /// <returns>resulting value</returns>
        public static T Min<T>(
            T obj1,
            T obj2)
            where T : IComparable<T>
        {
            return obj1.CompareTo(obj2) < 0 ? obj1 : obj2;
        }
    }
}
