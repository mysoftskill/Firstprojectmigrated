// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    ///     collection class helpers
    /// </summary>
    public static class ListHelper
    {
        /// <summary>Gets the empty list of type T</summary>
        /// <typeparam name="T">type to have an empty list of</typeparam>
        /// <returns>resulting value</returns>
        public static IList<T> EmptyList<T>()
        {
            return EmptyListHelper<T>.Instance;
        }

        /// <summary>
        ///     helper class to manage a cached instance of an empty list for places where an ICollection or
        ///      IList needs to be returned
        /// </summary>
        /// <typeparam name="T">type to have an empty list of</typeparam>
        private static class EmptyListHelper<T>
        {
            /// <summary>list data member</summary>
            private static IList<T> list;

            /// <summary>Gets the instance</summary>
            public static IList<T> Instance =>
                EmptyListHelper<T>.list ?? (EmptyListHelper<T>.list = new ReadOnlyCollection<T>(new List<T>()));
        }
    }
}
