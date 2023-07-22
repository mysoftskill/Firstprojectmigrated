// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    /// Privacy-Experience-Client Exception
    /// </summary>
    public class PrivacyExperienceClientException : Exception
    {
        /// <summary>
        /// Gets the error.
        /// </summary>
        public Error Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyExperienceClientException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        public PrivacyExperienceClientException(Error error) 
            : base(string.Format(CultureInfo.InvariantCulture, "Privacy-Experience-Service Error: {0}", error))
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyExperienceClientException" /> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="innerException">The inner exception.</param>
        public PrivacyExperienceClientException(Error error, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, "Privacy-Experience-Service Error: {0}", error), innerException)
        {
            this.Error = error;
        }
    }
}