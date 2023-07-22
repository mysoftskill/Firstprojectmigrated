// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISingleRecordBlobHelper<T>
    {
        /// <summary>
        ///     The name of the container for this blob helper.
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        ///     Create Record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        Task<string> CreateRecordAsync(T record);

        /// <summary>
        ///     Deletes the container and everything in it
        /// </summary>
        /// <returns>true if the container has been deleted, false if the container didn't exist</returns>
        Task<bool> DeleteContainerAsync();

        /// <summary>
        ///     Delete the record, return true if exists, false if already deleted
        /// </summary>
        /// <returns>true if record has been deleted, false if the record didn't exist</returns>
        Task<bool> DeleteRecordAsync();

        /// <summary>
        ///     Gets a record, assuming that InitializeAsync has been called.
        /// </summary>
        /// <param name="allowNotFound"></param>
        /// <returns>the record</returns>
        Task<T> GetRecordAsync(bool allowNotFound = false);

        /// <summary>
        ///     Initializes and Deletes a record for a key.
        ///     This is used when the class is not initialized with a key, for Listing all the records in the container.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if the record is deleted, false if record didn't exist</returns>
        Task<bool> InitializeAndDeleteAsync(string key);

        /// <summary>
        ///     Initializes the class to work with a particular record in the container.
        ///     If the key is null or whitespace, this class and still be used to list records in the container.
        ///     Initialize can be called later.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task InitializeAsync(string key);

        /// <summary>
        ///     lists records in the container in ascending order up to the max specified by top
        /// </summary>
        /// <param name="prefix">filters the selection of records</param>
        /// <param name="top">max number of records returned</param>
        /// <returns></returns>
        Task<IList<T>> ListRecordsAscendingAsync(string prefix, int top);

        /// <summary>
        ///     lists records in the container in descending order up to the max specified by top
        /// </summary>
        /// <param name="prefix">filters the selection of records</param>
        /// <param name="top">max number of records returned</param>
        /// <returns></returns>
        Task<IList<T>> ListRecordsDescendingAsync(string prefix, int top);

        /// <summary>
        ///     updates or inserts a record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        Task<string> UpsertRecordAsync(T record);
    }
}
