// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     This represents a single file in an export destination.
    /// </summary>
    public interface IExportFile : IDisposable
    {
        /// <summary>
        ///     Append data to the file.
        /// </summary>
        /// <param name="data">The data to append to the file.</param>
        /// <returns>Length of the stream appended to the file.</returns>
        Task<long> AppendAsync(Stream data);
    }
}
