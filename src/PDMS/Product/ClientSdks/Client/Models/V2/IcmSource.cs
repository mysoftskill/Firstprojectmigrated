namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a set of sources from ICM data.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<IcmSource>))]
    public enum IcmSource
    {
        /// <summary>
        /// Indicates that the value was manually set.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Indicates that the value was pulled from ServiceTree.
        /// </summary>
        ServiceTree = 1
    }
}