namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines a Command ID, which is a GUID.
    /// </summary>
    public sealed class CommandId : Identifier
    {
        /// <summary>
        /// Creates a new command ID from the given string, which is assumed to be a valid GUID.
        /// </summary>
        public CommandId(string value) : base(value)
        {
        }

        /// <summary>
        /// Creates a new command ID from the given GUID.
        /// </summary>
        public CommandId(Guid value) : base(value)
        {
        }
    }
}
