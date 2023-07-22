namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System;

    internal class CommandPageValidationException : Exception
    {
        public string CommandId { get; }

        public CommandPageValidationException(string commandId, Exception innerException) 
            : base($"Failed to validate command {commandId}", innerException)
        {
            CommandId = commandId;
        }
    }
}
