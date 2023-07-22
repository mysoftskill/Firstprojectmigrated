namespace Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Owin;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Middleware that randomly sets the Connection: close header on HTTP response messages.
    /// Well-behaved HTTP clients will treat this as a signal to tear down the TCP connection
    /// that transmitted this request and negotiate a new one.
    /// 
    /// The advantage of this approach is that it prevents connections from being permanently sticky
    /// to a single server, so that our machines have much more deterministic load balancing.
    /// </summary>
    internal class RandomConnectionCloseMiddleware : OwinMiddleware
    {
        private const int RandomClosePercentage = 1;
        
        public RandomConnectionCloseMiddleware(OwinMiddleware next) : base(next)
        {
        }

        /// <summary>
        /// Applies the Connection: close response header if conditions are met.
        /// </summary>
        public override async Task Invoke(IOwinContext context)
        {
            bool randomlyClose = RandomHelper.Next(0, 99) < RandomClosePercentage && 
                FlightingUtilities.IsEnabled(FlightingNames.RandomConnectionCloseEnable);

            if (randomlyClose || PrivacyApplication.Instance?.CancellationToken.IsCancellationRequested == true)
            {
                var response = context.Response;
                response.OnSendingHeaders(
                    state =>
                    {
                        // Per HTTP 1.1 RFC: http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
                        response.Headers["Connection"] = "close";
                    },
                    null);

                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ConnectionsClosed").Increment();
            }

            await this.Next.Invoke(context);
        }
    }
}
