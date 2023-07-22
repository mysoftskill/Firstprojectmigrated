namespace Microsoft.Azure.ComplianceServices.Common.AppConfig.Cache
{
    public interface ICache<T>
    {
        /// <summary>
        /// Clear all data in cache.
        /// </summary>
        void Reset();

        /// <summary>
        /// Retrieve total items in cache.
        /// </summary>
        /// <returns>long, number of items</returns>
        long Count();

        /// <summary>
        /// Add an item ( a key / value pair ) to cache.
        /// </summary>
        /// <param name="key">Key for the item.</param>
        /// <param name="value">Item to be added.</param>
        void AddItem(string key, T value);

        /// <summary>
        /// Retrieve an item from cache with the specified key.
        /// </summary>
        /// <param name="key"> Key for the item</param>
        /// <param name="item"> Reference to the item</param>
        /// <returns>True if the item was retried, false otherwise</returns>
        bool GetItem(string key, out T item);

    }
}
