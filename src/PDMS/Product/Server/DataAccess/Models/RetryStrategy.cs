namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the policy for retrying when an error occurs contacting an agent.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [JsonConverter(typeof(EnumTolerantConverter<RetryStrategy>))]
    public enum RetryStrategy
    {
        /// <summary>
        /// Indicates that a fixed interval should be used when retrying a call.
        /// </summary>
        FixedRetry = 1,

        /// <summary>
        /// Indicates that an exponentially increasing interval should be used when retrying a call.
        /// </summary>
        ExponentialRetry = 2
    }
}