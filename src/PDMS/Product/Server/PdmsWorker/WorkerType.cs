namespace Microsoft.PrivacyServices.DataManagement.Worker
{
    /// <summary>
    /// Classifies the type of a session.
    /// </summary>
    public enum WorkerType
    {
        /// <summary>
        /// Change Feed Reader.
        /// </summary>
        ChangeFeedReaderWorker,

        /// <summary>
        /// Data Owner Worker.
        /// </summary>
        DataOwnerWorker,

        /// <summary>
        /// ServiceTree Metadata Worker.
        /// </summary>
        ServiceTreeMetadataWorker
    }
}
