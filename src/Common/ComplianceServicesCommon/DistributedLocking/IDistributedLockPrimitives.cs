namespace Microsoft.Azure.ComplianceServices.Common.DistributedLocking
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a set of primitive operations for a distributed lock.
    /// </summary>
    public interface IDistributedLockPrimitives<T> where T : class
    {
        /// <summary>
        /// Gets a value indicating whether the primitive is locked or not.
        /// </summary>
        Task<DistributedLockStatus<T>> GetStatusAsync();

        /// <summary>
        /// Updates the contents of the lock, assuming the etag is valid.
        /// </summary>
        Task<bool> TryAcquireOrExtendLeaseAsync(T value, DateTimeOffset expirationTime, string ownerId, string etag);

        /// <summary>
        /// Creates the lock primitive if it does not exist.
        /// </summary>
        Task CreateIfNotExistsAsync();
    }
}
