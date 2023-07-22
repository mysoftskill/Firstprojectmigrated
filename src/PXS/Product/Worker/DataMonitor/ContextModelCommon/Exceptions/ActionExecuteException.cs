// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     exception thrown when an action execute failure occurs
    /// </summary>
    [Serializable]
    public class ActionExecuteException : ActionException
    {
        /// <summary>
        ///     Initializes a new instance of the ActionExecuteException class
        /// </summary>
        /// <param name="message">error message</param>
        public ActionExecuteException(string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ActionExecuteException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="inner">inner exception</param>
        public ActionExecuteException(
            string message,
            Exception inner) :
            base(message, inner)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the ActionExecuteException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected ActionExecuteException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
