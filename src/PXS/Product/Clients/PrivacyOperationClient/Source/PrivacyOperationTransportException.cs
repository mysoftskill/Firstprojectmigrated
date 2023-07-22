// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.PrivacyOperation.Client
{
    using System.Net;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    /// <summary>
    ///     PrivacyOperationTransportException
    /// </summary>
    public class PrivacyOperationTransportException : PrivacyOperationClientException
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyOperationTransportException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        public PrivacyOperationTransportException(Error error, HttpStatusCode httpStatusCode) : base(error)
        {
            this.HttpStatusCode = httpStatusCode;
        }
    }
}