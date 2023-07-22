namespace Microsoft.PrivacyServices.DataManagement.Worker.Scheduler
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// An interface that defines methods for worker.
    /// </summary>
    public interface IWorker
    {
        ISessionFactory SessionFactory { get; set; }
        IEventWriterFactory EventWriterFactory { get; set; }

        /// <summary>
        /// Time in milliseconds between runs
        /// </summary>
        int IdleTimeBetweenCallsInMilliseconds { get; }

        /// <summary>
        /// Name of lock used by this worker.
        /// </summary>
        string LockName { get; }

        /// <summary>
        /// How long the worker can hold the lock.
        /// </summary>
        double LockExpiryTimeInMilliseconds { get; }

        /// <summary>
        /// Maximum number of times to try acquiring the lock before giving up.
        /// </summary>
        int LockMaxFailureCountPerInstance { get; }

        /// <summary>
        /// Indicates whether the worker can aquire the lock (ie, is enabled)
        /// </summary>
        bool EnableAcquireLock { get; }

        /// <summary>
        /// Perform custom work.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>
        Task<string> DoWorkAsync(CancellationToken cancellationToken);
    }
}