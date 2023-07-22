// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     represents an unexpected command
    /// </summary>
    public class UnexpectedCommandException : Exception
    {
        /// <summary>Initializes a new instance of the UnexpectedCommandException class</summary>
        /// <param name="message">error message</param>
        public UnexpectedCommandException(
            string message) :
            base(message)
        {
        }

        /// <summary>Initializes a new instance of the UnexpectedCommandException class</summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public UnexpectedCommandException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>Initializes a new instance of the UnexpectedCommandException class</summary>
        public UnexpectedCommandException()
        {
        }

        /// <summary>Initializes a new instance of the UnexpectedCommandException class</summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected UnexpectedCommandException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
