namespace Microsoft.Azure.ComplianceServices.Common.Interfaces
{
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Describes the results of health statuses.
    /// </summary>
    public enum ServiceHealthStatus
    {
        /// <summary>
        /// All is well.
        /// </summary>
        OK = 0,

        /// <summary>
        /// Warning.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// A problem.
        /// </summary>
        Error = 2,
    }

    /// <summary>
    /// An interface that can authorize data agents and other services
    /// </summary>
    public interface IHostingEnvironment
    {
        /// <summary>
        /// Indicates if this a developer's onebox machine or not.
        /// </summary>
        bool IsDevMachine { get; }

        /// <summary>
        /// Gets the logical name of the environment.
        /// </summary>
        string EnvironmentName { get; }

        /// <summary>
        /// Gets the name of the current node. This can be a machine or otherwise, and is contextual depending on the type of IHostingEnvironment.
        /// </summary>
        string NodeName { get; }

        /// <summary>
        /// A task that waits for any environmental dependencies to be installed.
        /// </summary>
        Task WaitForDependenciesInstalledAsync();

        /// <summary>
        /// Creates a key vault client that the service can use to access secrets and certificates.
        /// </summary>
        IAzureKeyVaultClientFactory CreateKeyVaultClientFactory(string keyVaultBaseUrl, string clientId);

        /// <summary>
        /// For watchdogs, reports a status with the given name and message about the local machine.
        /// </summary>
        Task ReportServiceHealthStatusAsync(ServiceHealthStatus status, string name, string message);

        /// <summary>
        /// Gets or creates an <see cref="IPerformanceCounter"/> with the given category and name.
        /// </summary>
        IPerformanceCounter GetOrCreatePerformanceCounter(PerformanceCounterType type, string name);

        /// <summary>
        /// Gets or creates an <see cref="IAppConfiguration"/> instance.
        /// </summary>
        IAppConfiguration CreateAppConfiguration(ILogger logger);
    }
}
