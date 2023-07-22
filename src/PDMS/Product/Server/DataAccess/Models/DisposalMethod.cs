namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Enum describing data inventory disposal method.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [JsonConverter(typeof(EnumTolerantConverter<DisposalMethod>))]
    public enum DisposalMethod
    {
        /// <summary>
        /// Corresponds to the full delete disposal method.
        /// </summary>
        FullDelete = 1,

        /// <summary>
        /// Corresponds to the de-identify disposal method.
        /// </summary>
        DeIdentify = 2,

        /// <summary>
        /// Corresponds to other disposal method.
        /// </summary>
        Other = 3,
    }
}