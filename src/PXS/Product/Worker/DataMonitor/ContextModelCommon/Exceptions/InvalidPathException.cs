// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     base class for action exceptions
    /// </summary>
    public class InvalidPathException : ActionException
    {
        /// <summary>
        ///     Initializes a new instance of the InvalidPathExpression class
        /// </summary>
        /// <param name="message">error message</param>
        public InvalidPathException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the InvalidPathExpression class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public InvalidPathException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the InvalidPathExpression class
        /// </summary>
        public InvalidPathException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the InvalidPathExpression class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected InvalidPathException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
