// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test delegating handler.
    /// </summary>
    /// <remarks>
    /// REVISIT(nnaemeka) : move to a central place; or consodilate/replace with any existing implementation.
    /// </remarks>
    public class TestDelegatingHandler : DelegatingHandler
    {
        /// <summary>
        /// Gets or sets the a method to execute, right before <seealso cref="SendAsync(HttpRequestMessage,CancellationToken)"/>.
        /// </summary>
        public Func<HttpRequestMessage, Task> OnBeforeSendAsync { get; set; }

        /// <summary>
        /// Gets or sets the a method execute, instead of <seealso cref="SendAsync(HttpRequestMessage,CancellationToken)"/>.
        /// </summary>
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> OnOverrideSendAsync { get; set; }

        /// <summary>
        /// Gets or sets the a method to execute, right before <seealso cref="SendAsync(HttpRequestMessage,CancellationToken)"/>.
        /// </summary>
        public Func<HttpResponseMessage, Task> OnAfterSendAsync { get; set; }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>Returns <see cref="Task{TResult}"/>. The task object representing the asynchronous operation.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // allow callers to peek the http-request message.
            var onBeforeSendAsync = this.OnBeforeSendAsync;
            if (onBeforeSendAsync != null)
            {
                await onBeforeSendAsync(request);
            }

            // allow callers to override the http-response message.
            var response = default(HttpResponseMessage);
            var onOverrideSendAsync = this.OnOverrideSendAsync;
            if (onOverrideSendAsync == null)
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            else
            {
                response = await onOverrideSendAsync(request);
            }

            // allow callers to peek at the http-response message.
            var onAfterSendAsync = this.OnAfterSendAsync;
            if (onAfterSendAsync != null)
            {
                await onAfterSendAsync(response);
            }

            return response;
        }
    }
}
