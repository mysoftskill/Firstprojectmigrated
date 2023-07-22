// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     exception thrown when an action parse failure occurs
    /// </summary>
    [Serializable]
    public class ActionParseException : ActionException
    {
        /// <summary>
        ///     Initializes a new instance of the ActionParseException class
        /// </summary>
        /// <param name="message">error message</param>
        public ActionParseException(string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ActionParseException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="inner">inner exception</param>
        public ActionParseException(
            string message,
            Exception inner) :
            base(message, inner)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ActionParseException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected ActionParseException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
