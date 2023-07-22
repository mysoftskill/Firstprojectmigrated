namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Adds extra client-specific methods to the ExportCommand class.
    /// </summary>
    public partial class ExportCommand : IExportCommand
    {
        /// <summary>
        /// Initializes an export command from the given parameters.
        /// </summary>
        public ExportCommand(
            string commandId,
            string assetGroupId,
            string assetGroupQualifier,
            string verifier,
            string correlationVector,
            string leaseReceipt,
            DateTimeOffset approximateLeaseExpiration,
            DateTimeOffset createdTime,
            IPrivacySubject subject,
            string agentState,
            IEnumerable<DataTypeId> dataTypes,
            Uri azureBlobUri,
            ICommandFeedClient commandFeedClient,
            string cloudInstance = null)
            : base(
                CommandTypeName,
                commandId,
                assetGroupId,
                assetGroupQualifier,
                verifier,
                correlationVector,
                leaseReceipt,
                approximateLeaseExpiration,
                createdTime,
                subject,
                agentState,
                commandFeedClient,
                cloudInstance)
        {
            this.PrivacyDataTypes = dataTypes.ToList();
            this.AzureBlobContainerTargetUri = azureBlobUri;
        }
    }
}
