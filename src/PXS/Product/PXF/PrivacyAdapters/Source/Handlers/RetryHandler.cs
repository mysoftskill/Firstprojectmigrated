// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common;

    /// <summary>
    /// An HTTP client handler to retry an executed request. The detection and retry strategy are specified in the retry manager.
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly RetryManager retryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryHandler"/> class.
        /// </summary>
        /// <param name="retryManager">The retry manager.</param>
        public RetryHandler(RetryManager retryManager)
        {
            this.retryManager = retryManager;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1" />. The task object representing the asynchronous operation.
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await this.retryManager.ExecuteAsync(
                componentName: "RetryHandler",
                methodName: "SendAsync",
                taskFunc: async () => await base.SendAsync(request, cancellationToken));
        }
    }
}
