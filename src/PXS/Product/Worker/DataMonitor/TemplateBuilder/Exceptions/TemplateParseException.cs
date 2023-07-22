// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     exception thrown when a template parse failure occurs
    /// </summary>
    [Serializable]
    public class TemplateParseException : TemplateException
    {
        /// <summary>
        ///     Initializes a new instance of the TemplateParseException class
        /// </summary>
        /// <param name="message">error message</param>
        public TemplateParseException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the TemplateParseException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected TemplateParseException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
