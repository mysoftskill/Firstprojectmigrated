namespace Microsoft.PrivacyServices.CommandFeed.Validator.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Public/Sovereign cloud instances
    /// </summary>
    public static class CloudInstance
    {
        /// <summary>
        /// Public cloud only
        /// </summary>
        public const string Public = "Public";

        /// <summary>
        /// Azure cloud instance in Mooncake
        /// </summary>
        public const string AzureMoonCake = "CN.Azure.Mooncake";

        /// <summary>
        /// Azure cloud instance in Fairfax
        /// </summary>
        public const string AzureFairfax = "US.Azure.Fairfax";

        /// <summary>
        /// Add any new cloud instance to this list
        /// </summary>
        public static List<string> All { get; } = new List<string>
        {
            Public,
            AzureMoonCake,
            AzureFairfax
        };
    }
}
