// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Runtime.Serialization;
    using System.Web.Services.Protocols;
    using System.Xml;

    /// <summary>
    ///     Structure for error details returned by MSA identity service (IDSAPI).
    /// </summary>
    [Serializable]
    public class MsaIdentityServiceException : SoapException
    {
        /// <summary>
        ///     Gets or sets the description.
        /// </summary>
        /// <value>
        ///     The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets the value error code.
        /// </summary>
        public long ErrorCode { get; set; }

        /// <summary>
        ///     Gets or sets internal error text.
        /// </summary>
        public string InternalErrorText { get; set; }

        /// <summary>
        ///     Gets or sets internal error code.
        /// </summary>
        public long InternalErrorCode { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Web.Services.Protocols.SoapException" /> class with the specified exception message, exception code, URI that identifies
        ///     the piece of code that caused the exception, application-specific exception information, and reference to the root cause of the exception.
        /// </summary>
        /// <param name="message">A message that identifies the reason the exception occurred. This parameter sets the <see cref="P:System.Exception.Message" /> property.</param>
        /// <param name="code">
        ///     An <see cref="T:System.Xml.XmlQualifiedName" /> that specifies the type of error that occurred. This parameter sets the
        ///     <see cref="P:System.Web.Services.Protocols.SoapException.Code" /> property.
        /// </param>
        /// <param name="actor">
        ///     A URI that identifies the piece of code that caused the exception. Typically, this is a URL to an XML Web service method. This parameter sets the
        ///     <see cref="P:System.Web.Services.Protocols.SoapException.Actor" /> property.
        /// </param>
        /// <param name="role">
        ///     A URI that represents the XML Web service's function in processing the SOAP message. This parameter sets the
        ///     <see cref="P:System.Web.Services.Protocols.SoapException.Role" /> property.
        /// </param>
        /// <param name="detail">
        ///     An <see cref="T:System.Xml.XmlNode" /> that contains application specific exception information. This parameter sets the
        ///     <see cref="P:System.Web.Services.Protocols.SoapException.Detail" /> property.
        /// </param>
        /// <param name="subCode">An optional subcode for the SOAP fault. This parameter sets the <see cref="P:System.Web.Services.Protocols.SoapException.SubCode" /> property.</param>
        /// <param name="innerException">An exception that is the root cause of the exception. This parameter sets the <see cref="P:System.Exception.InnerException" /> property.</param>
        public MsaIdentityServiceException(string message, XmlQualifiedName code, string actor, string role, XmlNode detail, SoapFaultSubCode subCode, Exception innerException)
            : base(message, code, actor, role, detail, subCode, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Web.Services.Protocols.SoapException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The T:System.Runtime.Serialization.StreamingContext  that contains contextual information about the source or destination.</param>
        protected MsaIdentityServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
