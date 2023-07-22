// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Use to indicate an error related to an HTTP response. The exception message
    /// will contain the full details of the HTTP response, including the body.
    /// </summary>
    public class HttpResponseException : HttpRequestException
    {
        public HttpResponseMessage Response { get; private set; }

        public HttpResponseException()
            : base()
        {
        }

        public HttpResponseException(string message)
            : base(message)
        {
        }

        public HttpResponseException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public HttpResponseException(HttpResponseMessage response, string message)
            : base("Message: \"{0}\", Response: \"{1}\"".FormatInvariant(message, response.ToStringWithContent()))
        {
            this.Response = response;
        }
    }

    /// <summary>
    /// Use to indicate a response containing a non-success status code. The exception message
    /// will contain the full details of the HTTP response, including the body.
    /// </summary>
    public class NonsuccessHttpStatusCodeException : HttpResponseException
    {
        public HttpStatusCode StatusCode { get; private set; }

        public NonsuccessHttpStatusCodeException()
            : base()
        {
        }

        public NonsuccessHttpStatusCodeException(string message)
            : base(message)
        {
        }

        public NonsuccessHttpStatusCodeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public NonsuccessHttpStatusCodeException(HttpResponseMessage response)
            : base(response, "Response status code does not indicate success: {0} ({1})".FormatInvariant((int)response.StatusCode, response.StatusCode))
        {
            this.StatusCode = response.StatusCode;
        }
    }
}
