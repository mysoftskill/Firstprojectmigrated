namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing inventory data category.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
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