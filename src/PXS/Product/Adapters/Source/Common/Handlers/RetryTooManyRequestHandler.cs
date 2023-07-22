// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers
{
    using System.Net.Http;
    using System.Net;
    using System.Threading.Tasks;
    using System.Threading;
    using System;
    using System.Linq;
    using System.Diagnostics;

    public class RetryTooManyRequestHandler : DelegatingHandler
    {
        readonly int maxRetryCount;
        readonly TimeSpan maxRetryTimespan;

        /// <summary>
        /// A handler that retries 429 errors if present. The timing defaults to the Retry-After header 
        /// if present and falls back to an exponential retry if not present.
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retries to perform.</param>
        /// <param name="maxRetryTimespan">The maximum amount of time to spend retrying.</param>
        public RetryTooManyRequestHandler( int maxRetryCount=2, TimeSpan maxRetryTimespan=default)
        {
            this.maxRetryCount = maxRetryCount;
            this.maxRetryTimespan= maxRetryTimespan == default ? TimeSpan.FromSeconds(20) : maxRetryTimespan;
        }

        public RetryTooManyRequestHandler( HttpMessageHandler innerHandler, int maxRetryCount=2, TimeSpan maxRetryTimespan=default): base(innerHandler)
        {
            this.maxRetryCount = maxRetryCount;
            this.maxRetryTimespan= maxRetryTimespan == default ? TimeSpan.FromSeconds(20) : maxRetryTimespan;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            TimeSpan sleepTime;
            int retryCount = 0;
            Random rndOffset = new Random();
            var stopWatch = new Stopwatch();

            while (true)
            {
                // call SendAsync from the next handler in the handler pipeline.
                response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode != (HttpStatusCode)429 || retryCount == maxRetryCount || stopWatch.Elapsed > maxRetryTimespan)
                {
                    break;
                }

                if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues) && !String.IsNullOrEmpty(retryAfterValues.FirstOrDefault()))
                {
                    sleepTime = TimeSpan.FromSeconds(int.Parse(retryAfterValues.First()));
                }
                else
                {
                    sleepTime = TimeSpan.FromSeconds((int)Math.Pow(2, retryCount));
                }

                // Add in a random offset (up to 1 second) to try to avoid concurrent call limits.
                Thread.Sleep((int)sleepTime.TotalMilliseconds + rndOffset.Next(1000));

                retryCount++;
            }

            return response;
        }
    }
}