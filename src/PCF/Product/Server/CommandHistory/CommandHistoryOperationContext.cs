namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json.Linq;

    internal class CommandHistoryOperationContext
    {
        public CommandHistoryOperationContext(
            CommandHistoryFragmentTypes fragmentTypesRead, 
            CommandId commandId,
            string coreDocumentEtag)
        {
            this.FragmentTypesRead = fragmentTypesRead;
            this.CommandId = commandId;
            this.CoreDocumentEtag = coreDocumentEtag;
        }

        /// <summary>
        /// The flags from the read operation.
        /// </summary>
        public CommandHistoryFragmentTypes FragmentTypesRead { get; }

        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; }

        /// <summary>
        /// The document's etag.
        /// </summary>
        public string CoreDocumentEtag { get; }

        /// <summary>
        /// The etag for the audit blob.
        /// </summary>
        public string AuditBlobEtag { get; set; }

        /// <summary>
        /// The pointer to the audit blob.
        /// </summary>
        public BlobPointer AuditBlobPointer { get; set; }

        /// <summary>
        /// The etag for command status blob.
        /// </summary>
        public string StatusBlobEtag { get; set; }

        /// <summary>
        /// The pointer to the status blob.
        /// </summary>
        public BlobPointer StatusBlobPointer { get; set; }

        /// <summary>
        /// The etag for export destination blobs.
        /// </summary>
        public string ExportDestinationBlobEtag { get; set; }

        /// <summary>
        /// The pointer to the export destination blob.
        /// </summary>
        public BlobPointer ExportDestinationBlobPointer { get; set; }
    }
}
