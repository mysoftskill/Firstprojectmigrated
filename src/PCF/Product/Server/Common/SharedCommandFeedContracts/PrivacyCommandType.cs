namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    /// <summary>
    /// Enumerates privacy command types. Note: these values are significant. Do not change them, since they are sometimes serialized
    /// as integers.
    /// </summary>
    public enum PrivacyCommandType : int
    {
        /// <summary>
        /// An invalid default value.
        /// </summary>
        None = 0,

        /// <summary>
        /// A delete command.
        /// </summary>
        Delete = 1,

        /// <summary>
        /// An export command.
        /// </summary>
        Export = 2,

        /// <summary>
        /// An account close command.
        /// </summary>
        AccountClose = 3,

        /// <summary>
        /// An age out command.
        /// </summary>
        AgeOut = 4
    }
}
