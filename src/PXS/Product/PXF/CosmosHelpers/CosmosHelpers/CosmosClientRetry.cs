// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.CosmosHelpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.PrivacyServices.Common.Azure;


    /// <summary>
    ///     Client to interact with Cosmos using their Scope API and retries on failure
    /// </summary>
    public class CosmosClientRetry : ICosmosClient
    {
        private readonly RetryManager retryMgr;

        private readonly ICosmosClient inner;

        /// <summary>
        ///    Initializes a new instance of the CosmosClientRetry class
        /// </summary>
        /// <param name="retryConfig">retry configuration</param>
        /// <param name="inner">implementation of the ICosmosClient that will be retried</param>
        /// <param name="logger">Geneva trace logger</param>
        public CosmosClientRetry(
            IRetryStrategyConfiguration retryConfig,
            ILogger logger,
            ICosmosClient inner)
        {
            ArgumentCheck.ThrowIfNull(logger, nameof(logger));

            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

            this.retryMgr = new RetryManager(retryConfig, logger, CosmosTransientErrorDetector.Instance);
        }

        /// <summary>
        ///     Creates a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="expiry">Stream expiry time after which Cosmos will delete it</param>
        /// <param name="mode">controls the behavior if the file already exists</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task CreateAsync(
            string stream,
            TimeSpan? expiry,
            CosmosCreateStreamMode mode)
        {
            return this.RunWithRetry($"CreateAsync ({stream})", () => this.inner.CreateAsync(stream, expiry, mode));
        }

        /// <summary>
        ///     Creates a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task CreateAsync(string stream)
        {
            return this.RunWithRetry($"CreateAsync ({stream})", () => this.inner.CreateAsync(stream));
        }

        /// <summary>
        ///     deletes a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>task object for asynchronous execution</returns>
        /// <remarks>deletion puts the stream into a "recycled" state</remarks>
        public Task DeleteAsync(
            string stream,
            bool ignoreNotFound)
        {
            return this.RunWithRetry($"DeleteAsync ({stream})", () => this.inner.DeleteAsync(stream, ignoreNotFound));
        }

        /// <summary>
        ///     Sets the stream life time
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="lifetime">lifetime to set</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting value</returns>
        public Task SetLifetimeAsync(
            string stream,
            TimeSpan? lifetime,
            bool ignoreNotFound)
        {
            return this.RunWithRetry(
                $"SetLifetimeAsync ([{stream}] ==> [{lifetime}])",
                () => this.inner.SetLifetimeAsync(stream, lifetime, ignoreNotFound));
        }

        /// <summary>
        ///     Renames a stream
        /// </summary>
        /// <param name="stream">stream</param>
        /// <param name="target">target</param>
        /// <param name="allowOverwrite">true if overwrite is allowed; false otherwise</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     This can be used to change the directory a stream is located in, but only within the same cluster
        /// </remarks>
        public Task RenameAsync(
            string stream,
            string target,
            bool allowOverwrite,
            bool ignoreNotFound)
        {
            return this.RunWithRetry(
                $"RenameAsync ([{stream}] ==> [{target}])",
                () => this.inner.RenameAsync(stream, target, allowOverwrite, ignoreNotFound));
        }

        /// <summary>
        ///     Upload a new stream and add the data
        /// </summary>
        /// <param name="data">data to add to the stream</param>
        /// <param name="stream">full path to stream</param>
        /// <param name="expirationTime">Expiration time of the upload operation.</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task UploadAsync(
            byte[] data,
            string stream,
            TimeSpan expirationTime)
        {
            return this.RunWithRetry(
                $"UploadAsync ({stream})", 
                () => this.inner.UploadAsync(data, stream, expirationTime));
        }

        /// <summary>
        ///     Append data to an existing stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="data">data to add to the stream</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task AppendAsync(
            string stream,
            byte[] data)
        {
            return this.RunWithRetry($"AppendAsync ({stream})", () => this.inner.AppendAsync(stream, data));
        }

        /// <summary>
        ///     Reads the contents of a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>task object for asynchronous execution whose result contains the requested stream contents</returns>
        public async Task<Stream> ReadStreamAsync(
            string stream,
            bool ignoreNotFound)
        {
            Stream result = null;

            await this.RunWithRetry(
                $"ReadStreamAsync ({stream})",
                async () => { result = await this.inner.ReadStreamAsync(stream, ignoreNotFound); });

            return result;
        }

        /// <summary>
        ///     reads the content of a stream in chunks
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="offset">offset to start reading from</param>
        /// <param name="length">maximum length of data to read</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>task object for asynchronous execution whose result contains the requested stream contents</returns>
        public Task<DataInfo> ReadStreamAsync(
            string stream, 
            long offset,
            long length,
            bool ignoreNotFound)
        {
            return this.RunWithRetry(
                $"ReadStreamAsync ({stream})",
                () => this.inner.ReadStreamAsync(stream, offset, length, ignoreNotFound));
        }

        /// <summary>
        ///     determines if a stream exists or not
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>true if the stream currently exists; false otherwise</returns>
        public Task<bool> StreamExistsAsync(string stream)
        {
            return this.RunWithRetry($"StreamExistsAsync ({stream})", () => this.inner.StreamExistsAsync(stream));
        }
        
        /// <summary>
        ///     determines if a directory exists or not
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <returns>true if the directory currently exists; false otherwise</returns>
        public Task<bool> DirectoryExistsAsync(string directoryPath)
        {
            return this.RunWithRetry(
                $"DirectoryExistsAsync ({directoryPath})", 
                () => this.inner.DirectoryExistsAsync(directoryPath));
        }

        /// <summary>
        ///     gets the information for a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="allowIncompleteStream">allow incomplete streams to be returned</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>resulting stream info</returns>
        public Task<CosmosStreamInfo> GetStreamInfoAsync(
            string stream,
            bool allowIncompleteStream,
            bool ignoreNotFound)
        {
            return this.RunWithRetry(
                $"GetStreamInfoAsync ({stream})",
                () => this.inner.GetStreamInfoAsync(stream, allowIncompleteStream, ignoreNotFound));
        }

        /// <summary>
        ///     gets the list of items in a directory
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found; false otherwise</param>
        /// <returns>resulting stream info</returns>
        public Task<ICollection<CosmosStreamInfo>> GetDirectoryInfoAsync(
            string directoryPath,
            bool ignoreNotFound)
        {
            return this.RunWithRetry(
                $"GetDirectoryInfoAsync ({directoryPath})", 
                () => this.inner.GetDirectoryInfoAsync(directoryPath, ignoreNotFound));
        }

        /// <summary>
        ///      Runs the function with retry
        /// </summary>
        /// <param name="tag">method tag</param>
        /// <param name="method">method to run</param>
        /// <returns>resulting value</returns>
        private Task RunWithRetry(
            string tag,
            Func<Task> method)
        {
            return this.retryMgr.ExecuteAsync(nameof(inner), tag, method);
        }

        /// <summary>
        ///      Runs the function with retry
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="tag">method tag</param>
        /// <param name="method">method to run</param>
        /// <returns>resulting value</returns>
        private Task<T> RunWithRetry<T>(
            string tag,
            Func<Task<T>> method)
        {
            return this.retryMgr.ExecuteAsync(nameof(inner), tag, method);
        }

        public ClientTech ClientTechInUse()
        {
            return inner.ClientTechInUse();
        }

        /// <summary>
        ///     detects if exceptions thrown by Cosmos methods are transient or not
        /// </summary>
        private class CosmosTransientErrorDetector : ITransientErrorDetectionStrategy
        {
            /// <summary>
            ///      Gets a singleton instance of this class
            /// </summary>
            public static CosmosTransientErrorDetector Instance { get; } = new CosmosTransientErrorDetector();

            /// <summary>
            ///      determines whether the specified exception is transient
            /// </summary>
            /// <param name="e">exception to test</param>
            /// <returns>true if the exception is transient, false otherwise</returns>
            public bool IsTransient(Exception e)
            {
                // it makes me EXTREMELY unhappy to assume all exceptions are transient, but playing whack-a-mole about which 
                //  exceptions can be transient as more pop up makes me even more unhappy, especially with a turnaround time of
                //  at least several hours to fix.
                return true;
            }
        }
    }
}
