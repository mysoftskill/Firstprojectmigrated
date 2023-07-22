// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Net;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    /// Privacy-Experience-Transport Exception
    /// </summary>
    public class PrivacyExperienceTransportException : PrivacyExperienceClientException
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyExperienceTransportException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        public PrivacyExperienceTransportException(Error error, HttpStatusCode httpStatusCode) : base(error)
        {
            this.HttpStatusCode = httpStatusCode;
        }
    }
}