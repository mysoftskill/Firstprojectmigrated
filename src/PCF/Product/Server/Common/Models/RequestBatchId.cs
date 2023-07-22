namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines a request batch ID, which is a logical identifer that groups a set of individual commands.
    /// </summary>
    /// <remarks>
    /// Often, a user clicks a button to delete several data types at once. PXS expands this into N commands, with each of
    /// those N commands carrying a different Command ID. However, all of those commands from that user's button press
    /// will carry the same RequestBatchId. This is called RequestGuid in PXS.
    /// </remarks>
    public sealed class RequestBatchId : Identifier
    {
        /// <summary>
        /// Creates a new Agent ID from the given string, which is assumed to be a valid GUID.
        /// </summary>
        public RequestBatchId(string value) : base(value)
        {
        }

        /// <summary>
        /// Creates a new Agent ID from the given GUID.
        /// </summary>
        public RequestBatchId(Guid value) : base(value)
        {
        }
    }
}
