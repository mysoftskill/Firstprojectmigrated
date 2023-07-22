// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     exception thrown when a data path does not exist
    /// </summary>
    [Serializable]
    public class DataPathDoesNotExistException : TemplateBuildException
    {
        /// <summary>
        ///     Initializes a new instance of the DataPathDoesNotExistException class
        /// </summary>
        /// <param name="message">error message</param>
        public DataPathDoesNotExistException(
            string message) :
            base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the DataPathDoesNotExistException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected DataPathDoesNotExistException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}
