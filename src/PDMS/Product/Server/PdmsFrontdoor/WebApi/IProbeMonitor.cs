namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a probe check.
    /// </summary>
    public interface IProbeMonitor
    {
        /// <summary>
        /// A function that issues a probe check.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task to execute the probe.</returns>
        Task ProbeAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// A default probe that always succeeds once the service has started.
    /// </summary>
    public class DefaultProbe : IProbeMonitor
    {
        /// <summary>
        /// A function that always succeeds.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task to execute the probe.</returns>
        public Task ProbeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}