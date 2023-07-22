namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    /// <summary>
    /// Enum describing expansion of referenced entities in data owner.
    /// </summary>
    [Flags]
    public enum DataOwnerExpandOptions
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
        /// Include service tree meta data in the response.
        /// </summary>
        ServiceTree = 1 << 1
    }
}