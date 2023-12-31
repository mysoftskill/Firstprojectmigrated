namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// A map stored in cold storage; supports change tracking.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class CommandHistoryChangeTrackedMap<TKey, TValue> : 
        ICommandHistoryChangeTrackedObject,
        IEnumerable<KeyValuePair<TKey, TValue>>
        where TValue : ICommandHistoryChangeTrackedObject
    {
        private bool dirty;
        private readonly ConcurrentDictionary<TKey, TValue> map;

        /// <summary>
        /// Creates a new instance of CommandHistoryChangeTrackedMap.
        /// </summary>
        public CommandHistoryChangeTrackedMap(CommandId commandId, IDictionary<TKey, TValue> initialDictionary)
        {
            this.CommandId = commandId;

            if (initialDictionary != null)
            {
                this.map = new ConcurrentDictionary<TKey, TValue>(initialDictionary);
            }
            else
            {
                this.map = new ConcurrentDictionary<TKey, TValue>();
            }

            // Brand new things are considered dirty by default.
            this.dirty = true;
        }

        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; }

        /// <summary>
        /// The number of key value pairs in the map.
        /// </summary>
        public int Count => this.map.Count;

        /// <summary>
        /// Index access by key.
        /// </summary>
        public TValue this[TKey key]
        {
            get => this.map[key];

            set
            {
                this.map[key] = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Indicates whether the given key is present in the map.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return this.map.ContainsKey(key);
        }

        /// <summary>
        /// Indicates if this instance has been modified.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (this.dirty)
                {
                    return true;
                }

                foreach (var item in this.map.Values)
                {
                    if (item?.IsDirty == true)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the value indicated by the given key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.map.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an enumerator.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.map.GetEnumerator();
        }

        /// <summary>
        /// Implements the non-generic IEnumerator interface.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Clears the dirty flag.
        /// </summary>
        public void ClearDirty()
        {
            this.dirty = false;

            foreach (var item in this.map.Values)
            {
                item?.ClearDirty();
            }
        }
    }
}
