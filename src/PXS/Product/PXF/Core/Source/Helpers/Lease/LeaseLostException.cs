// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Lease
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     represents an unexpectd command
    /// </summary>
    public class LeaseLostException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the LeaseLostException class
        /// </summary>
        /// <param name="message">error message</param>
        public LeaseLostException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the LeaseLostException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public LeaseLostException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the LeaseLostException class
        /// </summary>
        public LeaseLostException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the LeaseLostException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected LeaseLostException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
