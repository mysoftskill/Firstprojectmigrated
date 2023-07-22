// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.Common.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public enum ClientTech
    {
        Adls,
        Other
    }

    public struct AdlsConfig
    {
        public string AccountName { get; }
        public string ClientAppId { get; }
        public X509Certificate2 Cert { get; }
        public string AccountSuffix { get; }
        public string TenantId { get; }

        public AdlsConfig(
            string accountName,
            string clientAppId,
            string accountSuffix,
            string tenantId,
            X509Certificate2 cert)
        {
            AccountName = accountName;
            ClientAppId = clientAppId;
            AccountSuffix = accountSuffix;
            TenantId = tenantId;
            Cert = cert;
        }
    }

    public class DataInfo
    {
        public byte[] Data { get; }
        public int Length { get; }

        public DataInfo(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }
    }

    /// <summary>
    ///     Cosmos create stream mode
    /// </summary>
    public enum CosmosCreateStreamMode
    {
        /// <summary>
        ///      throw if the stream exists
        /// </summary>
        ThrowIfExists = 0,

        /// <summary>
        ///      treat an existing file as a successful create
        /// </summary>
        OpenExisting,

        /// <summary>
        ///      delete an existing file and recreate
        /// </summary>
        CreateAlways,
    }

    public class StreamExistException: Exception
    {
        public StreamExistException()
        {
        }

        public StreamExistException(string message)
            : base(message)
        {
        }

        public StreamExistException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    ///     Interface for Cosmos Client
    /// </summary>
    public interface ICosmosClient
    {
        /// <summary>
        ///     Creates a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="expiry">stream expiry time after which Cosmos will delete it</param>
        /// <param name="mode">controls the behavior if the file already exists</param>
        /// <returns>task object for asynchronous execution</returns>
        Task CreateAsync(
            string stream,
            TimeSpan? expiry,
            CosmosCreateStreamMode mode);

        /// <summary>
        ///     Creates a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>task object for asynchronous execution</returns>
        Task CreateAsync(string stream);

        /// <summary>
        ///     Deletes a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the stream is not found</param>
        /// <returns>task object for asynchronous execution</returns>
        /// <remarks>deletion puts the stream into a "recycled" state</remarks>
        Task DeleteAsync(
            string stream, 
            bool ignoreNotFound = false);

        /// <summary>
        ///     Sets the stream life time
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="lifetime">lifetime to set</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting value</returns>
        Task SetLifetimeAsync(
            string stream,
            TimeSpan? lifetime,
            bool ignoreNotFound);

        /// <summary>
        ///     Renames a stream
        /// </summary>
        /// <param name="stream">source stream name</param>
        /// <param name="target">target stream name</param>
        /// <param name="allowOverwrite">true if overwrite is allowed; false otherwise</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     This can be used to change the directory a stream is located in, but only within the same cluster
        /// </remarks>
        Task RenameAsync(
            string stream,
            string target,
            bool allowOverwrite,
            bool ignoreNotFound);

        /// <summary>
        ///     Creates a new stream and populates it with data
        /// </summary>
        /// <param name="data">data to add to the stream</param>
        /// <param name="stream">full path to stream</param>
        /// <param name="expirationTime">
        ///     expiration time of the upload operation
        ///     a null value set the expiration time to 'never expires'
        /// </param>
        /// <returns>task object for asynchronous execution</returns>
        Task UploadAsync(
            byte[] data,
            string stream,
            TimeSpan expirationTime);

        /// <summary>
        ///     Appends data to an existing stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="data">data to add to the stream</param>
        /// <returns>task object for asynchronous execution</returns>
        Task AppendAsync(
            string stream,
            byte[] data);

        /// <summary>
        ///     Reads the contents of a stream
        /// </summary>
        /// <param name="stream">stream full path</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>task object for asynchronous execution whose result contains the stream contents</returns>
        Task<Stream> ReadStreamAsync(
            string stream,
            bool ignoreNotFound = false);

        /// <summary>
        ///     Reads the contents of a stream in chunks
        /// </summary>
        /// <param name="stream">stream full path</param>
        /// <param name="offset">offset to start reading from</param>
        /// <param name="length">maximum length of data to read</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>task with stream content</returns>
        Task<DataInfo> ReadStreamAsync(
            string stream,
            long offset,
            long length,
            bool ignoreNotFound = false);

        /// <summary>
        ///     Determines if given stream exists or not
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>true if the directory currently exists; false otherwise</returns>
        Task<bool> StreamExistsAsync(string stream);

        /// <summary>
        ///     Determines if a directory exists or not
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <returns>true if the directory currently exists; false otherwise</returns>
        Task<bool> DirectoryExistsAsync(string directoryPath);

        /// <summary>
        ///     Gets the information for a stream
        /// </summary>
        /// <param name="stream">stream full path</param>
        /// <param name="allowIncompleteStream">allow incomplete streams to be returned</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting stream info</returns>
        Task<CosmosStreamInfo> GetStreamInfoAsync(
            string stream,
            bool allowIncompleteStream,
            bool ignoreNotFound = false);

        /// <summary>
        ///     Gets the list of items in a directory
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting stream info</returns>
        Task<ICollection<CosmosStreamInfo>> GetDirectoryInfoAsync(
            string directoryPath,
            bool ignoreNotFound = false);

        /// <summary>
        /// Check what ClientTech is used to communicate to cosmos.
        /// Added only to allow flighting, will be removed once vccclient related 
        /// code is cleaned up.
        /// </summary>
        /// <returns>ClientTech in use</returns>
        ClientTech ClientTechInUse();
    }
}
