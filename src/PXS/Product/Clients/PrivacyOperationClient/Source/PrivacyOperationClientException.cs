// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.PrivacyOperation.Client
{
    using System;
    using System.Globalization;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    /// <summary>
    ///     PrivacyOperationClientException
    /// </summary>
    public class PrivacyOperationClientException : Exception
    {
        /// <summary>
        /// Gets the error.
        /// </summary>
        public Error Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyOperationClientException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        public PrivacyOperationClientException(Error error) 
            : base(string.Format(CultureInfo.InvariantCulture, "Privacy-Operation Error: {0}", error))
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyOperationClientException" /> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="innerException">The inner exception.</param>
        public PrivacyOperationClientException(Error error, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, "Privacy-Operation Error: {0}", error), innerException)
        {
            this.Error = error;
        }
    }
}