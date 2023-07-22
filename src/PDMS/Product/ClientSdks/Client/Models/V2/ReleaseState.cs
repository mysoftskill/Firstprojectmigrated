namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Identifies what state an entity is in for release purposes.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<ReleaseState>))]
    public enum ReleaseState
    {
        /// <summary>
        /// Indicates that the entity is available to the <c>preprod</c> environment.
        /// </summary>
        PreProd = 0,

        /// <summary>
        /// Indicates that the entity is available to the first flight ring,
        /// and all previous rings as well.
        /// </summary>
        Ring1 = 1,

        /// <summary>
        /// Indicates that the entity is available to the second flight ring,
        /// and all previous rings as well.
        /// </summary>
        Ring2 = 2,

        /// <summary>
        /// Indicates that the entity is available to the third flight ring,
        /// and all previous rings as well.
        /// </summary>
        Ring3 = 3,

        /// <summary>
        /// Indicates that the entity is available to all customers in the prod environment,
        /// and all previous flight rings as well.
        /// </summary>
        Prod = 100
    }
}