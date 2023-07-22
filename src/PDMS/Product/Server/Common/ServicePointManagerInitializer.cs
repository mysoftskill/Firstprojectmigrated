namespace Microsoft.PrivacyServices.DataManagement.Common
{
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// Initializes the service point manager for the service.
    /// </summary>
    public class ServicePointManagerInitializer : IInitializer
    {
        private readonly IServicePointManagerConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePointManagerInitializer" /> class.
        /// </summary>
        /// <param name="config">The configuration for this component.</param>
        public ServicePointManagerInitializer(IServicePointManagerConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Initializes the service point manager.
        /// </summary>
        /// <returns>A task to execute the behavior.</returns>
        public Task InitializeAsync()
        {
            ServicePointManager.DefaultConnectionLimit = this.config.DefaultConnectionLimit;
            ServicePointManager.DnsRefreshTimeout = this.config.DnsRefreshTimeout;
            ServicePointManager.EnableDnsRoundRobin = this.config.EnableDnsRoundRobin;
            ServicePointManager.MaxServicePointIdleTime = this.config.MaxServicePointIdleTime;
            ServicePointManager.MaxServicePoints = this.config.MaxServicePoints;
            ServicePointManager.ReusePort = this.config.ReusePort;
            ServicePointManager.UseNagleAlgorithm = this.config.UseNagleAlgorithm;
            
            // This is an optional configuration.
            if (this.config.MinThreads > 0)
            {
                ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
                ThreadPool.SetMinThreads(this.config.MinThreads, minIOC);
            }

            return Task.CompletedTask;
        }
    }
}
