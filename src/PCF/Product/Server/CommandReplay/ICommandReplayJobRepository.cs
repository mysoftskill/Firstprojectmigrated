namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface for command replay job storage
    /// </summary>
    public interface ICommandReplayJobRepository
    {
        /// <summary>
        /// Insert a new command replay job
        /// </summary>
        Task InsertAsync(ReplayJobDocument replayJob);

        /// <summary>
        /// Query for the given replay job record by job id
        /// </summary>
        Task<ReplayJobDocument> QueryAsync(string jobId);

        /// <summary>
        /// Uses the given etag to do a replacement of the given replay job document.
        /// And return the new etag of the document
        /// </summary>
        Task<string> ReplaceAsync(ReplayJobDocument replayJob, string etag);

        /// <summary>
        /// Pop the next item off from the job queue
        /// </summary>
        Task<ReplayJobDocument> PopNextItemAsync(TimeSpan leaseDuration);

        /// <summary>
        /// Initializes the repository, creating any needed resources.
        /// </summary>
        Task InitializeAsync();
    }
}
