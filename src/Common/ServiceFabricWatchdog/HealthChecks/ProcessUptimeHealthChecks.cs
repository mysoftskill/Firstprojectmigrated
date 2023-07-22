namespace WatchdogSvc
{
    using System;
    using System.Diagnostics;
    using System.Fabric.Health;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Process uptime health check.
    /// </summary>
    public class ProcessUptimeHealthCheck : HealthCheckBase
    {
        private static readonly TimeSpan MinUptime = TimeSpan.FromMinutes(3);
        private readonly string processName;

        /// <summary>
        /// Create process uptime health check.
        /// </summary>
        /// <param name="processName">Target process name.</param>
        public ProcessUptimeHealthCheck(string processName)
            : base("ProcessUptimeHealthCheck")
        {
            this.processName = processName;
        }

        /// <inheritdoc />
        protected override Task CheckCoreAsync()
        {
            ServiceEventSource.Current.Trace("ProcessUptimeHealthCheck.CheckCoresAsync()");
            var process = Process.GetProcessesByName(this.processName).FirstOrDefault();
            if (process == null)
            {
                this.HealthState = HealthState.Error;
                this.HealthDescription = $"Process {this.processName} is not running.";

                return Task.FromResult(true);
            }

            var uptime = DateTime.Now.Subtract(process.StartTime);
            this.HealthState = uptime > MinUptime ? HealthState.Ok : HealthState.Error;
            this.HealthDescription = uptime > MinUptime ? $"Process {this.processName} running good." : $"Process {this.processName} running less than ({(int)MinUptime.TotalSeconds}) seconds.";

            return Task.FromResult(true);
        }
    }
}
