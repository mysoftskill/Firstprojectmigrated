// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using System.Collections.Generic;

namespace Microsoft.Membership.MemberServices.Common.Collections
{
    /// <summary>
    ///     Implements cache logic for <see cref="CacheDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key involved for cache logic</typeparam>
    public interface ICacheLogic<TKey>
    {
        /// <summary>
        ///     Called when the dictionary is performing get on a key.
        /// </summary>
        /// <param name="key">Key that get was called on.</param>
        void OnGetKey(TKey key);

        /// <summary>
        ///     Called when the dictionary is performing set on a key.
        /// </summary>
        /// <param name="key">Key that is being set.</param>
        void OnSetKey(TKey key);

        /// <summary>
        ///     Gets the key that should be removed from the dictionary.
        /// </summary>
        /// <returns>The key to remove.</returns>
        TKey ToRemove();

        /// <summary>
        ///     Removes a key from the caching logic.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> if the key was removed, otherwise <c>false</c></returns>
        bool Remove(TKey key);

        /// <summary>
        ///     Clears the cache logic.
        /// </summary>
        void Clear();
    }

    /// <summary>
    ///     Shared code for <see cref="LinkedList{T}"/> backed <see cref="ICacheLogic{TKey}"/>.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract class BaseCacheLogic<TKey> : ICacheLogic<TKey>
    {
        protected LinkedList<TKey> ordering;
        protected readonly object orderingLock = new object();

        public BaseCacheLogic()
        {
            this.ordering = new LinkedList<TKey>();
        }

        public void Clear()
        {
            lock (orderingLock)
            {
                this.ordering.Clear();
            }
        }

        public abstract void OnGetKey(TKey key);

        public abstract void OnSetKey(TKey key);

        public bool Remove(TKey key)
        {
            lock (orderingLock)
            {
                return this.ordering.Remove(key);
            }
        }

        public abstract TKey ToRemove();
    }

    /// <summary>
    ///     Last Recently Modified cache logic, where keys are only updated on set.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class LastRecentlyModifiedCacheLogic<TKey> : BaseCacheLogic<TKey>
    {
        public override void OnGetKey(TKey key)
        {
            // no-op
        }

        public override void OnSetKey(TKey key)
        {
            // During occasions of large device delete DSR volume, we run into null reference errors.
            // This is an attempt to fix that.
            lock (orderingLock)
            {
                this.ordering.Remove(key);
                this.ordering.AddLast(key);
            }
        }

        public override TKey ToRemove()
        {
            return this.ordering.First.Value;
        }
    }

    /// <summary>
    /// Last Recently Used cache logic, where keys are updated on get and set.
    /// </summary>
    public class LastRecentlyUsedCacheLogic<TKey> : BaseCacheLogic<TKey>
    {
        public override void OnGetKey(TKey key)
        {
            lock (orderingLock)
            {
                // Move it to the end
                this.ordering.Remove(key);
                this.ordering.AddLast(key);
            }
        }

        public override void OnSetKey(TKey key)
        {
            this.OnGetKey(key);
        }

        public override TKey ToRemove()
        {
            return this.ordering.First.Value;
        }
    }
}
