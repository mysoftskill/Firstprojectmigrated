namespace Microsoft.PrivacyServices.CommandFeed.Client.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class RetryHandler : DelegatingHandler
    {
        private IBackOff backoff;
        private readonly int maxRetries;
        private readonly TimeSpan retryTimeout;
        private readonly Random random;

        private readonly TimeSpan defaultRetryTimeout = TimeSpan.FromSeconds(30);

        private readonly TimeSpan jitterRange = TimeSpan.FromMilliseconds(100);

        ///<summary>
        /// Initializes a new instance of the RetryHandler class.
        /// </summary>
        /// <param name="backoff">Backoff helper class that handles backoff.</param>
        /// <param name="maxRetries">The maximum number of retries. </param>
        /// <param name="retryTimeout">The timr out for the whole retry process.</param>
        public RetryHandler(IBackOff backoff, int maxRetries, TimeSpan retryTimeout=default)
        {
            this.backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
            this.maxRetries = maxRetries < 0 ? 0 : maxRetries;
            this.retryTimeout = retryTimeout == default ? defaultRetryTimeout : retryTimeout;
            this.random = new Random();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            var stopWatch = Stopwatch.StartNew();

            // Start retry if the response is 429
            for (int i = 0; i < this.maxRetries && response.StatusCode == (HttpStatusCode)429; i++)
            {
                // Reach retry timeout. Abort.
                if (stopWatch.ElapsedTicks >= retryTimeout.Ticks)
                {
                    break;
                }

                // If retry time is suggested in response and it's within allowed retry timeout, use it
                // Otherwise perform our own backoff
                TimeSpan delay;

                var retryAfterHeader = response.Headers.RetryAfter;
                if (retryAfterHeader != null && 
                    retryAfterHeader.Delta.HasValue && 
                    retryAfterHeader.Delta.Value.Ticks < retryTimeout.Ticks)
                {
                    delay = TimeSpan.FromTicks(retryAfterHeader.Delta.Value.Ticks);
                }
                
                else
                {
                    delay = backoff.Delay();
                }

                // Apply some jitter (0~100 ms)
                var jitter = TimeSpan.FromMilliseconds(this.random.NextDouble() * this.jitterRange.TotalMilliseconds);

                // Wait
                await Task.Delay(delay + jitter);

                // Retry request
                response = await base.SendAsync(request, cancellationToken);
            }

            // Reset backoff
            backoff.Reset();

            return response;
        }

    }
}
