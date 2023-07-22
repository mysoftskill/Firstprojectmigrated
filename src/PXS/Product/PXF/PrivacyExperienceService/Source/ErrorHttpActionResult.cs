// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;

    /// <summary>
    /// Authentication failure result.
    /// </summary>
    public class ErrorHttpActionResult : IHttpActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHttpActionResult"/> class.
        /// </summary>
        /// <param name="error">The error-information.</param>
        /// <param name="request">The http-request.</param>
        public ErrorHttpActionResult(Error error, HttpRequestMessage request)
        {
            this.Error = error;
            this.Request = request;
        }

        /// <summary>
        /// Gets the error information.
        /// </summary>
        public Error Error { get; private set; }

        /// <summary>
        /// Gets the http-request.
        /// </summary>
        public HttpRequestMessage Request { get; private set; }

        /// <summary>
        /// Creates an <see cref="System.Net.Http.HttpResponseMessage"/> asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that, when completed, contains the <see cref="System.Net.Http.HttpResponseMessage"/>.</returns>
        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var result = DisposableUtilities.SafeCreate<HttpResponseMessage>(
                () => this.Request.CreateErrorResponse(this.Error));

            return Task.FromResult(result);
        }
    }
}