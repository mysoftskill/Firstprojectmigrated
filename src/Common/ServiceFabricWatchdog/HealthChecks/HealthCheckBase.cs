namespace WatchdogSvc
{
    using System.Fabric.Health;
    using System.Threading.Tasks;

    /// <summary>
    /// Watchdog health check base class.
    /// </summary>
    public abstract class HealthCheckBase
    {
        /// <summary>
        /// Create health check base class.
        /// </summary>
        protected HealthCheckBase(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// Health check property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Health check state.
        /// </summary>
        public HealthState HealthState { get; set; }

        /// <summary>
        /// Health check description.
        /// </summary>
        public string HealthDescription { get; set; }

        /// <summary>
        /// Watchdog health check implementation.
        /// </summary>
        protected abstract Task CheckCoreAsync();

        /// <summary>
        /// Watchdog check.
        /// </summary>
        public async Task CheckAsync()
        {
            await this.CheckCoreAsync();
        }
    }
}
