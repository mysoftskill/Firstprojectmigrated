// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     The export staging file class is intended to facilitate writing a file to the staging container.
    ///     This class is acquired from the ExportStagingStorageHelper
    /// </summary>
    public interface IExportStagingFile : IDisposable
    {
        /// <summary>
        ///     gets the name of the blob within the staging container
        /// </summary>
        string FileName { get; }

        /// <summary>
        ///     this method returns a unique block name.
        /// </summary>
        string NextBlockName { get; }

        /// <summary>
        ///     The export staging file class saves the block list so that it can later be committed.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task AddBlockAsync(string content);

        /// <summary>
        ///     Saves a single binary buffer as a block to the Azure Block Blob
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        Task AddBlockAsync(byte[] buffer);

        /// <summary>
        ///     Once all the blocks are written, they must be committed.
        /// </summary>
        /// <returns></returns>
        Task CommitAsync();
    }
}
