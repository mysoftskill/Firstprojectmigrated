namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates the command feed request status code.
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// The request is succeeded with no error.
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// The request is failed with error that is expected.
        /// </summary>
        ExpectedFailure = 1,

        /// <summary>
        /// The request is failed with unexpected error.
        /// </summary>
        UnexpectedFailure = 2
    }
}