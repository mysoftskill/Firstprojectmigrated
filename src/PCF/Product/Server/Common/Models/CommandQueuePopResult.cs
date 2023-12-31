namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result of a call to <see cref="ICommandQueue.PopAsync(int, TimeSpan?, CommandQueuePriority)"/>.
    /// </summary>
    public class CommandQueuePopResult
    {
        /// <summary>
        /// Initializes a new instance of CommandQueuePopResult.
        /// </summary>
        public CommandQueuePopResult(List<PrivacyCommand> commands, IList<Exception> errors)
        {
            this.Commands = commands ?? new List<PrivacyCommand>();
            this.Errors = errors ?? new Exception[0];
        }

        /// <summary>
        /// The list of commands.
        /// </summary>
        public List<PrivacyCommand> Commands { get; }

        /// <summary>
        /// Any error that was encountered while getting these commands. Note: we use an exception return value
        /// instead of throwing so that we can report partial failures and partial success.
        /// </summary>
        public IList<Exception> Errors { get; }
    }
}
