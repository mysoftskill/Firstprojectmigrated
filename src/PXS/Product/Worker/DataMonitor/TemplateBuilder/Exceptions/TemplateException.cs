// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     base class for template exceptions
    /// </summary>
    public class TemplateException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the TemplateException class
        /// </summary>
        /// <param name="message">error message</param>
        public TemplateException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the TemplateException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        public TemplateException(
            string message,
            Exception exception) :
            base(message, exception)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the TemplateException class
        /// </summary>
        public TemplateException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the TemplateException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected TemplateException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
