namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    /// <summary>
    /// Defines an interface representing an age out command.
    /// </summary>
    public interface IAgeOutCommand : IPrivacyCommand
    {
        /// <summary>
        /// The last time the account was active.
        /// </summary>
        DateTimeOffset? LastActive { get; }
    }
}