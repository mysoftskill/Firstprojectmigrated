// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility
{
    using System;

    /// <summary>
    ///     exception thrown by a data action
    /// </summary>
    public class DataActionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the ActionException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="isFatal">value indicating whether the exception should be treated as fatal or not</param>
        public DataActionException(
            string message,
            bool isFatal) :
            base(message)
        {
            this.IsFatal = isFatal;
        }

        /// <summary>
        ///     Initializes a new instance of the ActionException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        /// <param name="isFatal">value indicating whether the exception should be treated as fatal or not</param>
        public DataActionException(
            string message,
            Exception exception,
            bool isFatal) :
            base(message, exception)
        {
            this.IsFatal = isFatal;
        }

        /// <summary>
        ///     Initializes a new instance of the ActionException class
        /// </summary>
        /// <param name="isFatal">value indicating whether the exception should be treated as fatal or not</param>
        public DataActionException(bool isFatal)
        {
            this.IsFatal = isFatal;
        }

        /// <summary>
        ///     Gets a value indicating whether this instance represents a fatal exception
        /// </summary>
        public bool IsFatal { get; }
    }
}
