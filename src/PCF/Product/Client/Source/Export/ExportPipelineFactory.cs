// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    ///     Contains factory methods for creating <see cref="ExportPipeline" />s.
    /// </summary>
    public static class ExportPipelineFactory
    {
        /// <summary>
        ///     Creates a new ExportPipeline given the azure destination.
        /// </summary>
        /// <param name="logger">The command feed logger to use.</param>
        /// <param name="destination">
        ///     Uri indicating the destination of the export.
        ///     This should be <see cref="ExportCommand.AzureBlobContainerTargetUri" />.
        /// </param>
        /// <param name="path">
        ///     The path within the container to export to.
        ///     This should be <see cref="ExportCommand.AzureBlobContainerPath" />.
        /// </param>
        /// <param name="enableCompression">True to write the output compressed.</param>
        /// <returns>A new ExportPipeline.</returns>
        public static ExportPipeline CreateAzureExportPipeline(CommandFeedLogger logger, Uri destination, string path, bool enableCompression = false)
        {
            var container = new CloudBlobContainer(destination).GetDirectoryReference(path ?? string.Empty);
            return CreateExportPipeline(logger, new AzureContainerExportDestination(container), enableCompression);
        }

        /// <summary>
        ///     Creates a new ExportPipeline given the azure destination.
        /// </summary>
        /// <param name="logger">The command feed logger to use.</param>
        /// <param name="destination">
        ///     Uri indicating the destination of the export.
        ///     This should be <see cref="ExportCommand.AzureBlobContainerTargetUri" />.
        /// </param>
        /// <returns>A new ExportPipeline.</returns>
        [Obsolete("Use CreateAzureExportPipeline() that takes a path parameter, sourced from IExportCommand.AzureBlobContainerPath")]
        public static ExportPipeline CreateAzureExportPipeline(CommandFeedLogger logger, Uri destination)
        {
            var container = new CloudBlobContainer(destination).GetDirectoryReference(string.Empty);
            return CreateExportPipeline(logger, new AzureContainerExportDestination(container));
        }

        /// <summary>
        ///     Creates a new ExportPipeline given the local disk path.
        /// </summary>
        /// <param name="logger">The command feed logger to use.</param>
        /// <param name="path">The path on the local disk to export to.</param>
        /// <returns>A new ExportPipeline.</returns>
        public static ExportPipeline CreateLocalDiskPipeline(CommandFeedLogger logger, string path)
        {
            return CreateExportPipeline(logger, new LocalDiskExportDestination(path));
        }

        /// <summary>
        ///     Creates a new ExportPipeline given the in memory <see cref="MemoryExportDestination" />.
        /// </summary>
        /// <param name="logger">The command feed logger to use.</param>
        /// <param name="destination">The in memory destination to export to.</param>
        /// <param name="isCompressed">Is compressed</param>
        /// <returns>A new ExportPipeline.</returns>
        public static ExportPipeline CreateMemoryPipeline(CommandFeedLogger logger, MemoryExportDestination destination, bool isCompressed)
        {
            return CreateExportPipeline(logger, destination, isCompressed);
        }

        /// <summary>
        ///     Creates a new ExportPipeline that targets the given destination.
        /// </summary>
        /// <param name="logger">The command feed logger to use.</param>
        /// <param name="destination">The destination to export to.</param>
        /// <param name="enableCompression">True to enable zip compression to the destination.</param>
        public static ExportPipeline CreateExportPipeline(CommandFeedLogger logger, IExportDestination destination, bool enableCompression = false)
        {
            if (enableCompression && !(destination is CompressedFileExportDestination))
            {
                destination = new CompressedFileExportDestination(destination);
            }

            return new ExportPipeline(new JsonExportSerializer(logger), destination, enableCompression);
        }
    }
}
