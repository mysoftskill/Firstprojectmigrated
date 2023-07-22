// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters
{
    using System;
    using System.Web;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    /// PxfAdapterException
    /// </summary>
    /// <seealso cref="System.Web.HttpException" />
    public class PxfAdapterException : HttpException
    {
        /// <summary>
        /// Partner Id.
        /// </summary>
        public string PartnerId { get; private set; }

        /// <summary>
        /// Gets the name of the partner.
        /// </summary>
        public string PartnerName { get; private set; }


        /// <summary>
        ///Adapter error code.
        /// </summary>
        public AdapterErrorCode AdapterErrorCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PxfAdapterException"/> class.
        /// </summary>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="partnerName">Name of the partner.</param>
        /// <param name="httpCode">The HTTP code.</param>
        /// <param name="message">The message.</param>
        public PxfAdapterException(string partnerId, string partnerName, int httpCode, string message) : base(httpCode, message)
        {
            this.PartnerId = partnerId;
            this.PartnerName = partnerName;
            this.AdapterErrorCode = AdapterErrorCode.Unknown;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PxfAdapterException" /> class.
        /// </summary>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="partnerName">Name of the partner.</param>
        /// <param name="adapterErrorCode">The adapter error code.</param>
        /// <param name="httpCode">The HTTP code.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PxfAdapterException(string partnerId, string partnerName, AdapterErrorCode adapterErrorCode, int httpCode, string message, Exception innerException) : base(httpCode, message, innerException)
        {
            this.PartnerId = partnerId;
            this.PartnerName = partnerName;
            this.AdapterErrorCode = adapterErrorCode;
        }
    }
}