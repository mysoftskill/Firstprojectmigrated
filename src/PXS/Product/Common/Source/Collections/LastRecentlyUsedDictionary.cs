// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Collections
{
    using System.Collections.Generic;

    public class LastRecentlyUsedDictionary<TKey, TValue> : CacheDictionary<TKey, TValue>
    {
        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyUsedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        public LastRecentlyUsedDictionary(int maxCapacity, IEqualityComparer<TKey> comparer)
            : base(maxCapacity, comparer, new LastRecentlyUsedCacheLogic<TKey>())
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyUsedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        public LastRecentlyUsedDictionary(int maxCapacity, int initialCapacity, IEqualityComparer<TKey> comparer)
            : base(maxCapacity, initialCapacity, comparer, new LastRecentlyUsedCacheLogic<TKey>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyUsedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        public LastRecentlyUsedDictionary(int maxCapacity)
            : base(maxCapacity, new LastRecentlyUsedCacheLogic<TKey>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyUsedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        public LastRecentlyUsedDictionary(int maxCapacity, int initialCapacity)
            : base(maxCapacity, initialCapacity, new LastRecentlyUsedCacheLogic<TKey>())
        {
        }
    }
}
