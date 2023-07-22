namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler
{
    using System.Threading.Tasks;

    /// <summary>
    /// Data access for lock related functionality.
    /// </summary>
    /// <typeparam name="T">Custom lock state.</typeparam>
    public interface ILockDataAccess<T>
    {
        /// <summary>
        /// Create lock data entry.
        /// </summary>
        /// <param name="lockData">Lock data instance.</param>
        /// <returns>The lock object with service generated values (e.g. ETag) populated.</returns>
        Task<Lock<T>> CreateAsync(Lock<T> lockData);

        /// <summary>
        /// Get lock data.
        /// </summary>
        /// <param name="lockName">Lock name.</param>
        /// <returns>The lock object for given lock name, null if no lock exist with given name.</returns>
        Task<Lock<T>> GetAsync(string lockName);

        /// <summary>
        /// Update lock data entry.
        /// </summary>
        /// <param name="lockData">Lock data instance.</param>
        /// <returns>The lock object with updated ETag.</returns>
        Task<Lock<T>> UpdateAsync(Lock<T> lockData);
    }
}
