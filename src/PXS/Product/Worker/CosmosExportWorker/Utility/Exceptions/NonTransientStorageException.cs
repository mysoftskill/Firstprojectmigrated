// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    ///     represents an non-transient storage exception
    /// </summary>
    public class NonTransientStorageException : IOException
    {
        /// <summary>Initializes a new instance of the NonTransientStorageException class</summary>
        /// <param name="message">error message</param>
        public NonTransientStorageException(
            string message) :
            base(message)
        {
        }

        /// <summary>Initializes a new instance of the NonTransientStorageException class</summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public NonTransientStorageException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>Initializes a new instance of the NonTransientStorageException class</summary>
        public NonTransientStorageException()
        {
        }

        /// <summary>Initializes a new instance of the NonTransientStorageException class</summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected NonTransientStorageException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
