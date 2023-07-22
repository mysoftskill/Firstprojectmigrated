// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        ///     Maintain the order that things are added or updated in the dictionary
        /// </summary>
        private readonly ICacheLogic<TKey> cacheLogic;

        /// <summary>
        ///     Internal dictionary implementation
        /// </summary>
        private readonly Dictionary<TKey, TValue> internalDictionary;

        /// <summary>
        ///     The maximum number of items that can be in the dictionary
        /// </summary>
        private readonly int maxCapacity;

        /// <inheritdoc />
        /// <summary>
        ///     Gets the count of items in the dictionary
        /// </summary>
        public int Count => this.internalDictionary.Count;

        /// <summary>
        ///     Gets if the dictionary is read only
        /// </summary>
        public bool IsReadOnly => false;

        /// <inheritdoc />
        /// <summary>
        ///     Gets or sets a value in the dictionary
        /// </summary>
        /// <param name="key">Key to get or set the value of</param>
        /// <returns>The value</returns>
        public TValue this[TKey key]
        {
            get
            {
                if (this.TryGetValue(key, out TValue val))
                {
                    return val;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                if (this.internalDictionary.ContainsKey(key))
                {
                    this.internalDictionary[key] = value;

                    // Move modified key to end
                    this.cacheLogic.OnSetKey(key);
                }
                else
                {
                    this.MaintainMaxElements(1);

                    this.internalDictionary[key] = value;
                    this.cacheLogic.OnSetKey(key);
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the keys in the dictionary
        /// </summary>
        public ICollection<TKey> Keys => this.internalDictionary.Keys;

        /// <inheritdoc />
        /// <summary>
        ///     Gets the values in the dictionary
        /// </summary>
        public ICollection<TValue> Values => this.internalDictionary.Values;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Common.Collections.LastRecentlyModifiedDictionary`2" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        /// <param name="cacheLogic">Logic to order cache</param>
        public CacheDictionary(int maxCapacity, IEqualityComparer<TKey> comparer, ICacheLogic<TKey> cacheLogic)
            : this(maxCapacity, cacheLogic)
        {
            this.internalDictionary = new Dictionary<TKey, TValue>(comparer);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Common.Collections.LastRecentlyModifiedDictionary`2" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        /// <param name="comparer">Implementation to use when comparing keys</param>
        /// <param name="cacheLogic">Logic to order cache</param>
        public CacheDictionary(int maxCapacity, int initialCapacity, IEqualityComparer<TKey> comparer, ICacheLogic<TKey> cacheLogic)
            : this(maxCapacity, cacheLogic)
        {
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            this.internalDictionary = new Dictionary<TKey, TValue>(initialCapacity, comparer);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyModifiedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="cacheLogic">Logic to order cache</param>
        public CacheDictionary(int maxCapacity, ICacheLogic<TKey> cacheLogic)
        {
            if (maxCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be greater than 0");
            }

            this.maxCapacity = maxCapacity;
            this.internalDictionary = new Dictionary<TKey, TValue>();
            this.cacheLogic = cacheLogic ?? throw new ArgumentNullException(nameof(cacheLogic));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LastRecentlyModifiedDictionary{TKey,TValue}" /> class
        /// </summary>
        /// <param name="maxCapacity">The maximum amount of items allowed in the dictionary</param>
        /// <param name="initialCapacity">The initial capacity of the dictionary</param>
        /// <param name="cacheLogic">Logic to order cache</param>
        public CacheDictionary(int maxCapacity, int initialCapacity, ICacheLogic<TKey> cacheLogic)
            : this(maxCapacity, cacheLogic)
        {
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), $"Cannot be greater than {nameof(maxCapacity)}");
            }

            this.internalDictionary = new Dictionary<TKey, TValue>(initialCapacity);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Adds a key value pair to the dictionary if its key is not already in the dictionary.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (!this.internalDictionary.ContainsKey(item.Key))
            {
                this.MaintainMaxElements(1);
            }

            // Dictionary will throw if the same key is attempted to be added again
            // so perform add to dictionary first since we expect and want to replicate
            // this behavior
            this.internalDictionary.Add(item.Key, item.Value);
            this.cacheLogic.OnSetKey(item.Key);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Adds a key and value to the dictionary if its key is not already in the dictionary.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        public void Add(TKey key, TValue value) => this.Add(new KeyValuePair<TKey, TValue>(key, value));

        /// <inheritdoc />
        /// <summary>
        ///     Clears the contents of the dictionary.
        /// </summary>
        public void Clear()
        {
            this.internalDictionary.Clear();
            this.cacheLogic.Clear();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines if they dictionary contains a specific key and value pair
        /// </summary>
        /// <param name="item">The key value pair to check if it is in the dictionary</param>
        /// <returns><c>true</c>if the pair is in the dictionary, otherwise <c>false</c></returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) => this.internalDictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value);

        /// <inheritdoc />
        /// <summary>
        ///     Determines if a specific key is contained in the dictionary
        /// </summary>
        /// <param name="key">The key to check for</param>
        /// <returns><c>true</c> if the key is found in the dictionary, otherwise <c>false</c></returns>
        public bool ContainsKey(TKey key) => this.internalDictionary.ContainsKey(key);

        /// <inheritdoc />
        /// <summary>
        ///     Copies values from the dictionary to the array starting at the specified index
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="arrayIndex">The array index to start at</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> pair in this.internalDictionary)
            {
                array[arrayIndex] = pair;
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the enumerator
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.internalDictionary.GetEnumerator();

        /// <inheritdoc />
        /// <summary>
        ///     Removes a specific key value pair from the dictionary
        /// </summary>
        /// <param name="item">The key value pair to remove</param>
        /// <returns><c>true</c> if removed, otherwise <c>false</c></returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (this.internalDictionary.TryGetValue(item.Key, out TValue value) && Equals(value, item.Value))
            {
                return this.Remove(item.Key);
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Removes a specific key value pair from the dictionary
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns><c>true</c> if removed, otherwise <c>false</c></returns>
        public bool Remove(TKey key)
        {
            if (this.internalDictionary.Remove(key))
            {
                this.cacheLogic.Remove(key);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Tries and gets a value out of the dictionary
        /// </summary>
        /// <param name="key">The key to get</param>
        /// <param name="value">The value if the key was contained in the dictionary</param>
        /// <returns><c>true</c> if the value was retrieved, otherwise <c>false</c></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (this.internalDictionary.TryGetValue(key, out value))
            {
                this.cacheLogic.OnGetKey(key);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the keys from the dictionary
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

        /// <inheritdoc />
        /// <summary>
        ///     Gets the values from the dictionary
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

        /// <inheritdoc />
        /// <summary>
        ///     Gets the enumerator for the dictionary
        /// </summary>
        /// <returns>The enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        ///     Removes items from the container for new items to be added.
        /// </summary>
        /// <param name="beingAdded">Number of items being added</param>
        private void MaintainMaxElements(int beingAdded)
        {
            if (beingAdded > this.maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(beingAdded));
            }

            while (this.Count + beingAdded > this.maxCapacity)
            {
                // Remove oldest element
                TKey toRemove = this.cacheLogic.ToRemove();
                this.internalDictionary.Remove(toRemove);
                this.cacheLogic.Remove(toRemove);

                --beingAdded;
            }
        }
    }
}
