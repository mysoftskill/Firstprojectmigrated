// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Collections
{
    using System.Collections.Generic;

    public class LastRecentlyModifiedDictionary<TKey, TValue> : CacheDictionary<TKey, TValue>
    {
        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Common.Collections.LastRecentlyModifiedDictionary`2" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        public LastRecentlyModifiedDictionary(int maxCapacity, IEqualityComparer<TKey> comparer)
            : base(maxCapacity, comparer, new LastRecentlyModifiedCacheLogic<TKey>())
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Common.Collections.LastRecentlyModifiedDictionary`2" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        public LastRecentlyModifiedDictionary(int maxCapacity, int initialCapacity, IEqualityComparer<TKey> comparer)
            : base(maxCapacity, initialCapacity, comparer, new LastRecentlyModifiedCacheLogic<TKey>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyModifiedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        public LastRecentlyModifiedDictionary(int maxCapacity)
            : base(maxCapacity, new LastRecentlyModifiedCacheLogic<TKey>())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyModifiedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        public LastRecentlyModifiedDictionary(int maxCapacity, int initialCapacity)
            : base(maxCapacity, initialCapacity, new LastRecentlyModifiedCacheLogic<TKey>())
        {
        }
    }
}
