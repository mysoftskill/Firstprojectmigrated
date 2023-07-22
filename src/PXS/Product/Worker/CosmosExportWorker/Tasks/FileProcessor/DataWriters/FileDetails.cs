// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    /// <summary>
    ///     contract for objects that 
    /// </summary>
    public interface IFileDetails
    {
        /// <summary>
        ///     Gets the file's product id
        /// </summary>
        string ProductId { get; }

        /// <summary>
        ///     Gets the file' name
        /// </summary>
        string FileName { get; }

        /// <summary>
        ///     Gets the number of bytes written for this file
        /// </summary>
        long Size { get; }

        /// <summary>
        ///     Gets the count of rows written for this file
        /// </summary>
        long RowCount { get; }
    }

    /// <summary>
    ///     contract for objects that can add data rows to files
    /// </summary>
    public interface IFileDataManager : IFileDetails
    {
        /// <summary>
        ///     Adds a row to the file
        /// </summary>
        /// <param name="json">json to add</param>
        /// <param name="onlyRecordStats">true to only record stats; false to record stats and any other required data</param>
        void AddRow(
            string json,
            bool onlyRecordStats);
    }

    /// <summary>
    ///     base implementation of the file details and row adder 
    /// </summary>
    public class FileDetails : IFileDataManager
    {
        /// <summary>
        ///     Initializes a new instance of the FileDetails class
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="productId">product id</param>
        public FileDetails(
            string fileName,
            string productId)
        {
            this.ProductId = productId;
            this.FileName = fileName;
        }

        /// <summary>
        ///     Gets the file's product id
        /// </summary>
        public string ProductId { get; }

        /// <summary>
        ///     Gets the file' name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     Gets the number of bytes written for this file
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        ///     Gets teh count of rows written for this file
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        ///     Adds a row to the file
        /// </summary>
        /// <param name="json">json to add</param>
        /// <param name="onlyRecordStats">true to only record stats; false to record stats and any other required data</param>
        public virtual void AddRow(
            string json,
            bool onlyRecordStats)
        {
            this.RowCount += 1;
            this.Size += json?.Length ?? 0;
        }
    }
}
