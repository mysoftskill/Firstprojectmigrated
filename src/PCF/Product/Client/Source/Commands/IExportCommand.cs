namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Defines an interface representing a range delete command.
    /// </summary>
    public interface IExportCommand : IPrivacyCommand
    {
        /// <summary>
        /// Gets the Azure container path the agent should write into. Do not write into any path within the
        /// <see cref="AzureBlobContainerTargetUri" /> than this path.
        /// </summary>
        string AzureBlobContainerPath { get; }

        /// <summary>
        /// Gets the Azure Blob container to which the exported data should be uploaded.
        /// </summary>
        Uri AzureBlobContainerTargetUri { get; }

        /// <summary>
        /// The data types that are to be exported.
        /// </summary>
        IEnumerable<DataTypeId> PrivacyDataTypes { get; }
    }
}
