namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines an Agent ID, which is a GUID.
    /// </summary>
    public sealed class AgentId : Identifier
    {
        /// <summary>
        /// Creates a new Agent ID from the given string, which is assumed to be a valid GUID.
        /// </summary>
        public AgentId(string value) : base(value, true)
        {
        }

        /// <summary>
        /// Creates a new Agent ID from the given GUID.
        /// </summary>
        public AgentId(Guid value) : base(value)
        {
        }
    }
}
