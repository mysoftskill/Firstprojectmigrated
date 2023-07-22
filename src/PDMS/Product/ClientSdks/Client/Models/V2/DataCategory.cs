namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing inventory data category.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(EnumTolerantConverter<DataCategory>))]
    public enum DataCategory
    {
        /// <summary>
        /// Corresponds to no data category of referenced entities.
        /// </summary>
        None = 0,

        /// <summary>
        /// Corresponds to the controller data category.
        /// </summary>
        Controller = 1 << 0,

        /// <summary>
        /// Corresponds to the processor data category.
        /// </summary>
        Processor = 1 << 1,
    }
}