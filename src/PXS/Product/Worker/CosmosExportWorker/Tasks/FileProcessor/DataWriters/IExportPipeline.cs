// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for writing to the export pipeline
    /// </summary>
    public interface IExportPipeline : IDisposable
    {
        /// <summary>
        ///     Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="productId">productId this data is from</param>
        /// <param name="fileName">name of the file to export to</param>
        /// <param name="jsonData">piece of data to append to the file as a properly formatted JSON serialized string</param>
        /// <returns>A task that completes when the export has been successful</returns>
        Task ExportAsync(
            string productId, 
            string fileName, 
            string jsonData);
    }
}
