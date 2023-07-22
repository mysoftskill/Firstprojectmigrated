namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the policy for retrying when an error occurs contacting an agent.
    /// </summary>
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