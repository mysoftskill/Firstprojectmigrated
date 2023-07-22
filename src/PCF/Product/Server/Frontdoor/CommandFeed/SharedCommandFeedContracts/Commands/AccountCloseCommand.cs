namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    /// <summary>
    /// An account close command from the privacy command feed.
    /// </summary>
    public partial class AccountCloseCommand : PrivacyCommand
    {
        /// <summary>
        /// The type of this command.
        /// </summary>
        public const string CommandTypeName = "accountClose";

        /// <summary>
        /// Initializes a new Account close command.
        /// </summary>
        public AccountCloseCommand() : base(CommandTypeName)
        {
        }
    }
}
