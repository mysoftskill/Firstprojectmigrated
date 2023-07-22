// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.Handlers
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
        private RetryManager retryManager;

        public RetryHandler(RetryManager retryManager)
        {
            this.retryManager = retryManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await this.retryManager.ExecuteAsync(
                componentName: "RetryHandler",
                methodName: "SendAsync",
                taskFunc: async () => await base.SendAsync(request, cancellationToken));
        }
    }
}
