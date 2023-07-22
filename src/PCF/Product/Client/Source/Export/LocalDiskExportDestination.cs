// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     An export destination that represents a folder on the local disk.
    /// </summary>
    public class LocalDiskExportDestination : IExportDestination
    {
        private readonly string path;

        /// <summary>
        ///     Creates a local export destination to the given folder.
        /// </summary>
        /// <param name="path">The path to export to. Will be created (but not cleared) if it already exists.</param>
        public LocalDiskExportDestination(string path)
        {
            this.path = path;
        }

        /// <inheritdoc />
        public Task<IExportFile> GetOrCreateFileAsync(string fileNameWithExtension)
        {
            string fullFileName = Path.Combine(this.path, fileNameWithExtension);
            string fullPath = Path.GetDirectoryName(fullFileName);
            Directory.CreateDirectory(fullPath);
            FileStream stream = File.Open(fullFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            return Task.FromResult<IExportFile>(new StreamExportFile(stream));
        }
    }
}
