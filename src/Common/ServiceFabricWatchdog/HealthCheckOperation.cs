namespace WatchdogSvc
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class HealthCheckOperations : IDisposable
    {
        private static readonly string ApplicationName = Environment.GetEnvironmentVariable("Fabric_ApplicationName");

        private readonly WatchdogSvc watchdogSvc;
        private readonly TimeSpan reportInterval;
        private readonly CancellationToken token;
        private readonly List<HealthCheckBase> healthChecks;
        private readonly Timer healthCheckTimer;

        internal FabricClient Client => this.watchdogSvc.Client;

        internal StatelessServiceContext Context => this.watchdogSvc.Context;

        internal HealthState OperatationHealthState { get; private set; } = HealthState.Ok;

        internal string OperationHealthDesc { get; private set; } = "OK";

        public HealthCheckOperations(
            WatchdogSvc watchdogSvc, TimeSpan interval, CancellationToken token)
        {
            this.watchdogSvc = watchdogSvc ?? throw new ArgumentNullException("Argument 'watchdogSvc' is null.");
            this.token = token;
            this.reportInterval = interval;
            this.healthChecks = new List<HealthCheckBase>();

            if (InitializeHealthChecks())
            {
                ServiceEventSource.Current.Trace($"InitializeHealthChecks Done at: {DateTimeOffset.UtcNow}");
                // Create a timer that calls the local method every X seconds from now.
                this.healthCheckTimer = new Timer(
                    async (o) =>
                    {
                        try
                        {
                            ServiceEventSource.Current.Trace($"RunHealthChecksAsync Start at: {DateTimeOffset.UtcNow}");
                            await this.RunHealthChecksAsync();
                            this.UpdateOpertaionHealth(HealthState.Ok);
                        }
                        catch (Exception ex)
                        {
                            this.UpdateOpertaionHealth(HealthState.Error, ex.Message);
                        }
                    },
                    this.token,
                    interval,
                    interval);
            }
        }

        private bool InitializeHealthChecks()
        {
            // Validate and initialize ProcessUpTimeHealthCheck
            if (WatchdogParams.TryParseUptimeCheckEnabled(out bool isUptimeCheckEnabled))
            {
                if (isUptimeCheckEnabled)
                {
                    healthChecks.Add(new ProcessUptimeHealthCheck(WatchdogParams.UptimeCheckProcessName));
                }
            }
            else
            {
                this.UpdateOpertaionHealth(HealthState.Error, "Failed to parse config UptimeCheckEnabled as bool");
                return false;
            }

            // Validate and initialize ProbeCheckHealthCheck
            if (WatchdogParams.TryParseProbeCheckEnabled(out bool isProbeCheckEnabled))
            {
                if (isProbeCheckEnabled)
                {
                    if (WatchdogParams.TryParseProbeCheckUri(out Uri probeUri))
                    {
                        healthChecks.Add(new UriProbeHealthCheck(probeUri));
                    }
                    else
                    {
                        this.UpdateOpertaionHealth(HealthState.Error, "Failed to parse config ProbeCheckUri as Uri");
                        return false;
                    }
                }
            }
            else
            {
                this.UpdateOpertaionHealth(HealthState.Error, "Failed to parse config ProbeCheckEnabled as bool");
                return false;
            }

            return true;
        }

        private void UpdateOpertaionHealth(HealthState hs, string hsDesc = "OK")
        {
            this.OperatationHealthState = hs;
            this.OperationHealthDesc = hsDesc;
        }

        private async Task RunHealthChecksAsync()
        {
            Task.WaitAll(this.healthChecks.Select(w => w.CheckAsync())?.ToArray());

            foreach (var hc in this.healthChecks)
            {
                ServiceEventSource.Current.Trace($"{hc.PropertyName}-{hc.HealthState}-{hc.HealthDescription}");
                await ReportHealthToServiceFabricAsync(hc);
            }
        }

        private async Task ReportHealthToServiceFabricAsync(HealthCheckBase hc)
        {
            Uri applicationName = new Uri(ApplicationName);

            try
            {
                HealthReport healthReport = null;

                // Report on the service level health report
                if (!string.IsNullOrEmpty(WatchdogParams.TargetServiceManifestName))
                {
                    var servicePackageList = await this.Client.QueryManager.GetDeployedServicePackageListAsync(
                        this.watchdogSvc.Context.NodeContext.NodeName,
                        applicationName,
                        WatchdogParams.TargetServiceManifestName,
                        TimeSpan.FromSeconds(30),
                        token);

                    if (servicePackageList?.Count > 0)
                    {
                        HealthInformation hi = new HealthInformation(this.Context.ServiceName.AbsoluteUri, hc.PropertyName, hc.HealthState)
                        {
                            TimeToLive = this.reportInterval.Add(TimeSpan.FromSeconds(60)),
                            Description = hc.HealthDescription,
                            RemoveWhenExpired = false,
                            SequenceNumber = HealthInformation.AutoSequenceNumber,
                        };

                        healthReport = new DeployedServicePackageHealthReport(
                            applicationName,
                            WatchdogParams.TargetServiceManifestName,
                            servicePackageList.FirstOrDefault().ServicePackageActivationId,
                            this.Context.NodeContext.NodeName,
                            hi);
                    }
                }

                // In case service level report failed, try the application level.
                if (healthReport == null)
                {
                    HealthInformation hi = new HealthInformation(this.Context.ServiceName.AbsoluteUri, hc.PropertyName, hc.HealthState)
                    {
                        TimeToLive = this.reportInterval.Add(TimeSpan.FromSeconds(30)),
                        Description = hc.HealthDescription,
                        RemoveWhenExpired = true, // Since report on Application level is not periodically, remove it once it is expired.
                        SequenceNumber = HealthInformation.AutoSequenceNumber,
                    };

                    healthReport = new DeployedApplicationHealthReport(
                        applicationName,
                        this.Context.NodeContext.NodeName,
                        hi);
                }

                Client.HealthManager.ReportHealth(healthReport);
                ServiceEventSource.Current.Trace("Published health report");
            }
            catch (FabricObjectClosedException)
            {
                this.watchdogSvc.RefreshFabricClient();
                ServiceEventSource.Current.Trace("FabricClient closed");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Trace(ex.StackTrace);
                throw;
            }

        }

        #region IDisposable Support

        private bool disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.healthCheckTimer.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }

        #endregion
    }
}
