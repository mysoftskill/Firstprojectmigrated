namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    /// <summary>
    /// Enum describing expansion of referenced entities in data agent.
    /// </summary>
    [Flags]
    public enum DataAgentExpandOptions
    {
        /// <summary>
        /// Corresponds to no expansion of referenced entities.
        /// </summary>
        None = 0,

        /// <summary>
        /// Include tracking details in the response.
        /// </summary>
        TrackingDetails = 1 << 0,

        /// <summary>
        /// Include the calculated boolean HasSharingRequests in the response.
        /// </summary>
        HasSharingRequests = 1 << 1,
    }
}