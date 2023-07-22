// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.CosmosHelpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;


    /// <summary>
    ///     Client to interact with Cosmos using their Scope API and tracks the calls via SLL
    /// </summary>
    public class CosmosClientInstrumented : ICosmosClient
    {
        private const string DependencyType = "Cosmos";

        private const string OperationVersionV1 = "1";

        private const string PartnerId = "Cosmos";

        private readonly ICosmosClient inner;

        /// <summary>
        ///     Initializes a new instance of the CosmosClientInstrumented class
        /// </summary>
        /// <param name="inner">implementation of the ICosmosClient whose behavior will be instrumented</param>
        public CosmosClientInstrumented(ICosmosClient inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
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
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Create",
                HttpMethod.Post,
                () => this.inner.CreateAsync(stream, expiry, mode));
        }

        /// <summary>
        ///     Creates a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task CreateAsync(string stream)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Create",
                HttpMethod.Post,
                () => this.inner.CreateAsync(stream));
        }

        /// <summary>
        ///     Deletes a stream
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>task object for asynchronous execution</returns>
        /// <remarks>deletion puts the stream into a "recycled" state</remarks>
        public Task DeleteAsync(
            string stream,
            bool ignoreNotFound)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Delete",
                HttpMethod.Delete,
                () => this.inner.DeleteAsync(stream, ignoreNotFound));
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
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "SetLifetime",
                HttpMethod.Put,
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
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Rename",
                HttpMethod.Post,
                () => this.inner.RenameAsync(stream, target, allowOverwrite, ignoreNotFound));
        }

        /// <summary>
        ///     Creates a new stream and populates it with data
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
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Upload",
                HttpMethod.Post,
                () => this.inner.UploadAsync(data, stream, expirationTime));
        }

        /// <summary>
        ///     Appends data to an existing stream.
        /// </summary>
        /// <param name="stream">Stream full path.</param>
        /// <param name="data">Date we are appending.</param>
        /// <returns>task object for asynchronous execution</returns>
        public Task AppendAsync(
            string stream,
            byte[] data)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "Append",
                HttpMethod.Post,
                () => this.inner.AppendAsync(stream, data));
        }

        /// <summary>
        ///     Reads the contents of a stream
        /// </summary>
        /// <param name="sourceStream">Stream full path.</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>Task with stream content.</returns>
        public Task<Stream> ReadStreamAsync(
            string sourceStream,
            bool ignoreNotFound)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                sourceStream,
                "ReadStream",
                HttpMethod.Get,
                () => this.inner.ReadStreamAsync(sourceStream, ignoreNotFound));
        }

        /// <summary>
        ///     Reads the content of a stream in chunks
        /// </summary>
        /// <param name="sourceStream">stream full path</param>
        /// <param name="offset">offset to start reading from</param>
        /// <param name="length">maximum length of data to read</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>Task with stream content</returns>
        public Task<DataInfo> ReadStreamAsync(
            string sourceStream,
            long offset,
            long length,
            bool ignoreNotFound)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                sourceStream,
                "ReadStream",
                HttpMethod.Get,
                () => this.inner.ReadStreamAsync(sourceStream, offset, length, ignoreNotFound));
        }

        /// <summary>
        ///     Determines if given stream exists or not
        /// </summary>
        /// <param name="stream">full path to stream</param>
        /// <returns>true if the directory currently exists; false otherwise</returns>
        public Task<bool> StreamExistsAsync(string stream)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "StreamExists",
                HttpMethod.Get,
                () => this.inner.StreamExistsAsync(stream));
        }

        /// <summary>
        ///     Determines if a directory exists or not
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <returns>true if the directory currently exists; false otherwise</returns>
        public Task<bool> DirectoryExistsAsync(string directoryPath)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                directoryPath,
                "DirectoryExists",
                HttpMethod.Get,
                () => this.inner.DirectoryExistsAsync(directoryPath));
        }

        /// <summary>
        ///     Gets the information for a stream
        /// </summary>
        /// <param name="stream">stream full path</param>
        /// <param name="allowIncompleteStream">allow incomplete streams to be returned</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting stream info</returns>
        public Task<CosmosStreamInfo> GetStreamInfoAsync(
            string stream,
            bool allowIncompleteStream,
            bool ignoreNotFound)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                stream,
                "GetStreamInfo",
                HttpMethod.Get,
                () => this.inner.GetStreamInfoAsync(stream, allowIncompleteStream, ignoreNotFound));
        }

        /// <summary>
        ///     Gets the list of items in a directory
        /// </summary>
        /// <param name="directoryPath">full path to the directory containing the stream</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting stream info</returns>
        public Task<ICollection<CosmosStreamInfo>> GetDirectoryInfoAsync(
            string directoryPath,
            bool ignoreNotFound)
        {
            return CosmosClientInstrumented.InstrumentOutgoingCosmosEventAsync(
                directoryPath,
                "GetDirectoryInfo",
                HttpMethod.Get,
                () => this.inner.GetDirectoryInfoAsync(directoryPath, ignoreNotFound));
        }

        /// <summary>
        ///     performs the Cosmos method call with telemetry tracking 
        /// </summary>
        /// <param name="stream">stream</param>
        /// <param name="operationName">operation name</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <param name="method">method</param>
        /// <returns>resulting value</returns>
        private static async Task InstrumentOutgoingCosmosEventAsync(
            string stream, 
            string operationName, 
            HttpMethod httpMethod, 
            Func<Task> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                CosmosClientInstrumented.PartnerId,
                operationName,
                CosmosClientInstrumented.OperationVersionV1,
                stream,
                httpMethod,
                CosmosClientInstrumented.DependencyType);

            eventWrapper.Start();

            try
            {
                await method().ConfigureAwait(false);
                eventWrapper.Success = true;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }

        /// <summary>
        ///     performs the Cosmos method call with telemetry tracking 
        /// </summary>
        /// <typeparam name="TResult">type of the result</typeparam>
        /// <param name="stream">stream</param>
        /// <param name="operationName">operation name</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <param name="method">method</param>
        /// <returns>resulting value</returns>
        private static async Task<TResult> InstrumentOutgoingCosmosEventAsync<TResult>(
            string stream, 
            string operationName, 
            HttpMethod httpMethod, 
            Func<Task<TResult>> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                CosmosClientInstrumented.PartnerId,
                operationName,
                CosmosClientInstrumented.OperationVersionV1,
                stream,
                httpMethod,
                CosmosClientInstrumented.DependencyType);

            eventWrapper.Start();

            try
            {
                TResult result = await method().ConfigureAwait(false);
                eventWrapper.Success = true;
                return result;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }

        public ClientTech ClientTechInUse()
        {
            return inner.ClientTechInUse();
        }
    }
}
