namespace WatchdogSvc
{
    using System;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WatchdogSvc : StatelessService
    {
        private static readonly TimeSpan HealthReportInterval = TimeSpan.FromSeconds(30);

        private static FabricClient client = null;

        private HealthCheckOperations healthCheckOperations = null;

        /// <summary>
        /// Service Fabric client instance
        /// </summary>
        public FabricClient Client => client;

        /// <summary>
        /// Static WatchdogSvc constructor.
        /// </summary>
        static WatchdogSvc()
        {
            client = new FabricClient();
        }

        public WatchdogSvc(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Refreshes the FabricClient instance.
        /// </summary>
        public void RefreshFabricClient()
        {
            FabricClient old = Interlocked.CompareExchange<FabricClient>(ref client, new FabricClient(), client);
            old?.Dispose();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            this.healthCheckOperations = new HealthCheckOperations(
                    this,
                    HealthReportInterval,
                    cancellationToken);

            while (cancellationToken.IsCancellationRequested == false)
            {
                // Report the health of the watchdog to Service Fabric.
                this.CheckWatchdogHealth();
                await Task.Delay(HealthReportInterval, cancellationToken);
            }
        }

        private void CheckWatchdogHealth()
        {
            if (this.healthCheckOperations == null)
            {
                ReportLocalPartitionHealth("HealthCheckOperations", HealthState.Error, "healthCheckOperations initialization failed");
            }
            else
            {
                ReportLocalPartitionHealth("HealthCheckOperations", this.healthCheckOperations.OperatationHealthState, this.healthCheckOperations.OperationHealthDesc);
            }

            ReportLocalPartitionHealth("WatchdogSvcHealth", HealthState.Ok, "Report WatchdogSvc itself health");
        }

        private void ReportLocalPartitionHealth(string propertyName, HealthState hs, string healthDescription)
        {
            HealthInformation hi = new HealthInformation(this.Context.ServiceName.AbsoluteUri, propertyName, hs)
            {
                // TimeToLive need to be larger than report interval if RemoveWhenExpired is false
                // This way if report is expired, which could mean watchdog stucked, will auto mark health as error.
                TimeToLive = HealthReportInterval.Add(TimeSpan.FromMinutes(1)),
                Description = healthDescription,
                RemoveWhenExpired = false,
                SequenceNumber = HealthInformation.AutoSequenceNumber,
            };

            this.Partition.ReportPartitionHealth(hi);
        }
    }
}
