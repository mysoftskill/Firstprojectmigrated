// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     exception thrown when a template build failure occurs
    /// </summary>
    [Serializable]
    public class TemplateBuildException : TemplateException
    {
        /// <summary>
        ///     Initializes a new instance of the TemplateBuildException class
        /// </summary>
        /// <param name="message">error message</param>
        public TemplateBuildException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the TemplateBuildException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected TemplateBuildException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
