namespace Microsoft.PrivacyServices.DataManagement.Worker.Scheduler
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;

    /// <summary>
    /// Worker class providing lock functionality to make sure that only one instance
    /// of worker is performing work at any given time.
    /// </summary>
    /// <typeparam name="T">Custom lock state.</typeparam>
    public abstract class LockWorker<T> : IWorker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockWorker{T}"/> class.
        /// </summary>
        /// <param name="id">Lock id for current process. This id should be generated 
        /// when process starts and reused in all subsequent LockWorker instance creation.</param>
        /// <param name="dateFactory">Date factory instance.</param>
        /// <param name="dataAccess">Lock data access.</param>
        /// <param name="sessionFactory">Session factory.</param>
        public LockWorker(Guid id, IDateFactory dateFactory, ILockDataAccess<T> dataAccess, ISessionFactory sessionFactory)
        {
            this.Id = id;
            this.DateFactory = dateFactory;
            this.DataAccess = dataAccess;
            this.SessionFactory = sessionFactory;
        }

        /// <summary>
        /// Gets lock id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets lock data access.
        /// </summary>
        public ILockDataAccess<T> DataAccess { get; }

        /// <summary>
        /// Gets date factory.
        /// </summary>
        protected IDateFactory DateFactory { get; }

        public ISessionFactory SessionFactory { get; set; }

        public IEventWriterFactory EventWriterFactory { get; set; }

        public abstract string LockName { get; set; }

        public abstract double LockExpiryTimeInMilliseconds { get; }

        public abstract bool EnableAcquireLock { get; set; }

        public abstract int LockMaxFailureCountPerInstance { get; }

        /// <inheritdoc />
        public abstract int IdleTimeBetweenCallsInMilliseconds { get; }
        
        /// <summary>
        /// Try to acquire lock, if lock is acquired then invoke DoLockWorkAsync and return true, 
        /// otherwise return false.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>
        public async Task<string> DoWorkAsync(CancellationToken cancellationToken)
        {
            Lock<T> lockStatus = null;

            try
            {
                var result = await this.SessionFactory.InstrumentAsync(
                    $"{this.LockName}.DoWorkAsync",
                    SessionType.Incoming,
                    async () =>
                    {
                        if (!this.EnableAcquireLock)
                        {
                            return new Tuple<Lock<T>, string>(null, "Disabled");
                        }

                        string callbackStatus = "WaitingForLock";
                        lockStatus = await this.AcquireLockAsync().ConfigureAwait(false);

                        if (lockStatus != null)
                        {
                            callbackStatus = await this.DoLockWorkAsync(lockStatus, cancellationToken).ConfigureAwait(false);
                        }

                        return new Tuple<Lock<T>, string>(lockStatus, callbackStatus);
                    }).ConfigureAwait(false);

                return result.Item2;
            }
            catch
            {
                // Update the failure count on the lock.
                // Only do this if this is an instance of the worker that is acting on the lock.
                if (lockStatus != null)
                {
                    lockStatus = await this.DataAccess.GetAsync(this.LockName).ConfigureAwait(false);

                    lockStatus.FailureCount += 1;

                    await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);
                }

                // The exception will have been logged by the InstrumentAsync code above.
                // So we can just suppress it, but we should back off to avoid running immediately.
                return "UnhandledException";
            }
        }

        /// <summary>
        /// Abstract method to be implemented in derived class to perform custom action once lock is acquired.
        /// </summary>
        /// <param name="lockStatus">Lock status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>
        public abstract Task<string> DoLockWorkAsync(Lock<T> lockStatus, CancellationToken cancellationToken);

        /// <summary>
        /// Try to acquire lock. Return null if some other process is holding lock.
        /// </summary>
        /// <returns>The Task object for asynchronous execution.</returns>
        private async Task<Lock<T>> AcquireLockAsync()
        {
            Lock<T> lockStatus = await this.DataAccess.GetAsync(this.LockName).ConfigureAwait(false);
            if (lockStatus == null
                || lockStatus.ExpiryTime < this.DateFactory.GetCurrentTime()
                || lockStatus.WorkerId == this.Id)
            {

                try
                {
                DateTimeOffset newExpiryTime =
                    this.DateFactory.GetCurrentTime().AddMilliseconds(this.LockExpiryTimeInMilliseconds);
                if (lockStatus == null)
                {
                    lockStatus = new Lock<T>
                    {
                        Id = this.LockName,
                        WorkerId = this.Id,
                        ExpiryTime = newExpiryTime,
                    };

                    lockStatus = await this.DataAccess.CreateAsync(lockStatus).ConfigureAwait(false);
                }
                else if (lockStatus.FailureCount >= this.LockMaxFailureCountPerInstance)
                {
                    // Clear failure count and release the lock.
                    lockStatus.FailureCount = 0;
                    lockStatus.ExpiryTime = this.DateFactory.GetCurrentTime();

                    await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);

                    lockStatus = null; // Force a backoff to ensure a different worker has a chance to pickup the lock.
                }
                else
                {
                    lockStatus.WorkerId = this.Id;
                    lockStatus.ExpiryTime = newExpiryTime;
                    lockStatus = await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);
                }
            }
            catch (DocumentClientException exn)
            {
                // Catch specific exception (Creation failed, ETag mismatch during update) and set lockStatus as null, do not throw.
                if ((exn.StatusCode == HttpStatusCode.PreconditionFailed) ||
                    (exn.StatusCode == HttpStatusCode.Conflict))
                {
                    lockStatus = null;
                }
                else
                {
                    throw;
                }
                }
            }
            else
            {
                lockStatus = null;

            }

            return lockStatus;
        }
    }
}
