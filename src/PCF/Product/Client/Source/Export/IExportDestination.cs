// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Threading.Tasks;

    /// <summary>
    ///     This represents the destination for a single export request.
    /// </summary>
    public interface IExportDestination
    {
        /// <summary>
        ///     Within the destination, get or create a file.
        /// </summary>
        /// <param name="fileNameWithExtension">The name of the file, including the extension.</param>
        /// <returns>An <see cref="IExportFile" /> representing the file that can be streamed to.</returns>
        Task<IExportFile> GetOrCreateFileAsync(string fileNameWithExtension);
    }
}
