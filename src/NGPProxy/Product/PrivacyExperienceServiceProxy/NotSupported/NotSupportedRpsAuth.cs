// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Web;

    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    ///     NotSupported instance of RpsAuth
    /// </summary>
    public class NotSupportedRpsAuth : IRpsAuthServer
    {
        /// <inheritdoc />
        public RpsAuthResult ValidateConsent(
            string siteName,
            string ticket,
            RpsTicketType ticketType,
            string[] scopes,
            out bool[] consents,
            out bool[] granular,
            out string[] contexts,
            RpsPropertyBag validationPropertyBag = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsPropertyBag ServerConfig
        {
            get { throw new NotSupportedException(); }
        }

        /// <inheritdoc />
        public RpsAuthResult GetApplicationAuthResult(string siteName, string ticket, RpsTicketType ticketType = RpsTicketType.Compact, RpsPropertyBag validationPropertyBag = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsAuthResult GetAuthResult(string siteName, string ticket, RpsTicketType ticketType = RpsTicketType.Compact, RpsPropertyBag validationPropertyBag = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsAuthResult GetHttpAuthResult(string siteName, HttpRequest request, RpsPropertyBag validationPropertyBag = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsAuthResult GetS2SSiteAuthResult(string siteName, string ticket, byte[] clientCertData, RpsPropertyBag validationPropertyBag = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsAuthResult ValidateConsent(string siteName, string ticket, string[] scopes)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public RpsAuthResult ValidateConsent(string siteName, string ticket, string[] scopes, out bool[] consents, out bool[] granular, out string[] contexts)
        {
            throw new NotSupportedException();
        }
    }
}
