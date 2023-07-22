// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    public enum ChunkedReadErrorCode
    {
        /// <summary>
        ///     unknown error
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Cosmos returned an empty stream even though the stream length reported by Cosmos is larger than the amount
        ///      currently read
        /// </summary>
        EarlyStreamEnd,

        /// <summary>
        ///    Cosmos returned an empty stream but we have read more data than the stream length reported by Cosmos
        /// </summary>
        ExtendedStreamLength,
    }

    /// <summary>
    ///     chunked read exception
    /// </summary>
    [Serializable]
    public class ChunkedReadException : IOException
    {
        private const string ErrorCodePropName = "ChunkedReadErrorCode";

        /// <summary>
        ///     Initializes a new instance of the CosmosChunkedReadException class
        /// </summary>
        /// <param name="errorCode">error code</param>
        /// <param name="path">file path</param>
        /// <param name="name">file name</param>
        /// <param name="size">file size</param>
        /// <param name="requestOffset">offset at which the read was requested</param>
        /// <param name="requestSize">number of bytes to read starting at offset</param>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public ChunkedReadException(
            ChunkedReadErrorCode errorCode,
            string path = null,
            string name = null,
            long size = 0L,
            long requestOffset = 0L,
            long requestSize = 0L,
            string message = null,
            Exception exception = null) :
            base(message, exception)
        {
            this.ErrorCode = errorCode;

            this.FilePath = path;
            this.FileName = name;
            this.FileSize = size;

            this.RequestOffset = requestOffset;
            this.RequestedSize = requestSize;
        }

        /// <summary>
        ///     Initializes a new instance of the CosmosChunkedReadException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected ChunkedReadException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
            this.RequestedSize = serializationInfo.GetInt64(nameof(this.RequestedSize));
            this.RequestOffset = serializationInfo.GetInt64(nameof(this.RequestOffset));

            this.FilePath = serializationInfo.GetString(nameof(this.FilePath));
            this.FileName = serializationInfo.GetString(nameof(this.FilePath));
            this.FileSize = serializationInfo.GetInt64(nameof(this.FileSize));

            this.ErrorCode = (ChunkedReadErrorCode)serializationInfo.GetInt32(ChunkedReadException.ErrorCodePropName);
        }

        /// <summary>
        ///     Gets error code
        /// </summary>
        public ChunkedReadErrorCode ErrorCode { get; }

        /// <summary>
        ///     Gets the path to the file being read
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Gets the name of the file being read
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     Gets the size of the file in bytes
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        ///     Gets the offset at which the read was requested
        /// </summary>
        public long RequestOffset { get; }

        /// <summary>
        ///     Gets the number of bytes to read starting at offset
        /// </summary>
        public long RequestedSize { get; }

        /// <summary>
        ///     When overridden in a derived class, sets the SerializationInfo with information about the exception
        /// </summary>
        /// <param name="info">serialization info</param>
        /// <param name="context">streaming context</param>
        public override void GetObjectData(
            SerializationInfo info, 
            StreamingContext context)
        {
            info.AddValue(ChunkedReadException.ErrorCodePropName, (int)this.ErrorCode);

            info.AddValue(nameof(this.FilePath), this.FilePath);
            info.AddValue(nameof(this.FileName), this.FileName);
            info.AddValue(nameof(this.FileSize), this.FileSize);

            info.AddValue(nameof(this.RequestOffset), this.RequestOffset);
            info.AddValue(nameof(this.RequestedSize), this.RequestedSize);

            base.GetObjectData(info, context);
        }
    }
}
