namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    /// <summary>
    /// Enum describing expansion of referenced entities in asset group.
    /// </summary>
    [Flags]
    public enum AssetGroupExpandOptions
    {
        /// <summary>
        /// Corresponds to no expansion of referenced entities.
        /// </summary>
        None = 0,

        /// <summary>
        /// Include tracking details in the response.
        /// </summary>
        TrackingDetails = 1 << 0,
    }
}