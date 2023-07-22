// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    /// <summary>
    ///     Set of file partitions
    /// </summary>
    public enum FileSizePartition
    {
        /// <summary>
        ///     invalid option
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     zero length files
        /// </summary>
        /// <remarks>
        ///     zero length files are special in that it can take Cosmos time to report the correct size of a file
        ///      so a zero length file could be any of the other options (or it could legitimately be a zero length
        ///      file)
        /// </remarks>
        Empty,

        /// <summary>
        ///     small files
        /// </summary>
        Small,

        /// <summary>
        ///     medium files
        /// </summary>
        Medium,

        /// <summary>
        ///     large files
        /// </summary>
        Large,

        /// <summary>
        ///     oversized files
        /// </summary>
        Oversize,
    }
}
