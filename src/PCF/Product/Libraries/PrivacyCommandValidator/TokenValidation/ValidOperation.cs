namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    /// <summary>
    /// Valid operations for the command
    /// </summary>
    public enum ValidOperation
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// Delete
        /// </summary>
        Delete,

        /// <summary>
        /// Export
        /// </summary>
        Export,

        /// <summary>
        /// AccountClose
        /// </summary>
        AccountClose,

        /// <summary>
        /// AgeOut
        /// </summary>
        AgeOut,

        /// <summary>
        /// A delete operation scoped to data types in the verifier token
        /// </summary>
        ScopedDelete,

        /// <summary>
        /// AccountCleanup for traversed user in multi-tenant collaboration scenario
        /// </summary>
        AccountCleanup,
    }
}
