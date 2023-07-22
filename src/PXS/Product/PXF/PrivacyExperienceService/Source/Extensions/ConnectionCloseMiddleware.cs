namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Owin;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Middleware that sets the Connection close header on HTTP response messages.
    ///     Well-behaved HTTP clients will treat this as a signal to tear down the TCP connection
    ///     that transmitted this request and negotiate a new one.
    ///     The advantage of this approach is that it prevents connections from being permanently sticky
    ///     to a single server, so that our machines have much more deterministic load balancing.
    /// </summary>
    internal class ConnectionCloseMiddleware : OwinMiddleware
    {
        private const string ComponentName = nameof(ConnectionCloseMiddleware);

        private const int RandomClosePercentage = 1;

        private readonly ICounterFactory counterFactory;

        private readonly ILogger logger;

        private readonly IRandom random;

        private readonly IPrivacyExperienceServiceConfiguration serviceConfiguration;

        public ConnectionCloseMiddleware(
            OwinMiddleware next,
            ICounterFactory counterFactory,
            ILogger logger,
            IRandom random,
            IPrivacyConfigurationManager configurationManager)
            : base(next)

        {
            this.serviceConfiguration = configurationManager.PrivacyExperienceServiceConfiguration;
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        ///     Applies the Connection: close response header if conditions are met.
        /// </summary>
        public override async Task Invoke(IOwinContext context)
        {
            bool randomlyClose = this.random.Next(0, 99) < RandomClosePercentage && this.serviceConfiguration.RandomConnectionCloseEnable;

            if (ConsoleHost.Instance?.CancellationToken.IsCancellationRequested == true || randomlyClose)
            {
                IOwinResponse response = context.Response;
                response.OnSendingHeaders(
                    state =>
                    {
                        // Per HTTP 1.1 RFC: http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html
                        response.Headers["Connection"] = "close";
                    },
                    null);

                this.counterFactory.GetCounter(CounterCategoryNames.PrivacyExperienceServiceConnections, "ConnectionCloseHeaderSent", CounterType.Number).Increment();
                this.logger.Information(ComponentName, "Connection close header sent to requester {0} with request URI:{1}", context.Request.LocalIpAddress, context.Request.Uri);
            }

            await this.Next.Invoke(context).ConfigureAwait(false);
        }
    }
}
