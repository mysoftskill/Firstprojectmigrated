namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;

    /// <summary>
    /// Data access layer for lock data (lock and reader state).
    /// </summary>
    /// <typeparam name="T">The lock status type.</typeparam>
    public class LockDataAccess<T> : ILockDataAccess<T>
    {
        private readonly DocumentModule.DocumentContext documentContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockDataAccess{T}"/> class.
        /// </summary>
        /// <param name="documentClient">DocumentDB client.</param>
        /// <param name="configuration">Configuration for DocumentDB.</param>
        public LockDataAccess(IDocumentClient documentClient, IDocumentDatabaseConfig configuration)
        {
            this.documentContext = new DocumentModule.DocumentContext
            {
                CollectionName = configuration.EntityCollectionName,
                DatabaseName = configuration.DatabaseName,
                DocumentClient = documentClient,
            };
        }

        /// <summary>
        /// Create lock data entry.
        /// </summary>
        /// <param name="lockData">Lock data instance.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public Task<Lock<T>> CreateAsync(Lock<T> lockData)
        {
            return DocumentModule.Create(lockData, this.documentContext);
        }

        /// <summary>
        /// Get lock data.
        /// </summary>
        /// <param name="lockName">Lock name.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public Task<Lock<T>> GetAsync(string lockName)
        {
            if (string.IsNullOrEmpty(lockName))
            {
                throw new ArgumentNullException(nameof(lockName));
            }

            return DocumentModule.Read<Lock<T>>(lockName, this.documentContext);
        }

        /// <summary>
        /// Update lock data entry.
        /// </summary>
        /// <param name="lockData">Lock data instance.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public Task<Lock<T>> UpdateAsync(Lock<T> lockData)
        {
            return DocumentModule.Update(lockData, this.documentContext);
        }
    }
}
