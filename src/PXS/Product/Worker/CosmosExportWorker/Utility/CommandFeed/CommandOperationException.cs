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
    public class CommandOperationException : Exception
    {
        /// <summary>Initializes a new instance of the CommandQueryException class</summary>
        /// <param name="message">error message</param>
        public CommandOperationException(
            string message) :
            base(message)
        {
        }

        /// <summary>Initializes a new instance of the CommandQueryException class</summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public CommandOperationException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>Initializes a new instance of the CommandQueryException class</summary>
        public CommandOperationException()
        {
        }

        /// <summary>Initializes a new instance of the CommandQueryException class</summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected CommandOperationException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
