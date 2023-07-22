namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing data inventory retention policy.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [JsonConverter(typeof(EnumTolerantConverter<RetentionPolicy>))]
    public enum RetentionPolicy
    {
        /// <summary>
        /// Corresponds to the five years retention policy.
        /// </summary>
        FiveYears = 1,

        /// <summary>
        /// Corresponds to the eighteen months retention policy.
        /// </summary>
        EighteenMonths = 2,

        /// <summary>
        /// Corresponds to the thirteen months retention policy.
        /// </summary>
        ThirteenMonths = 3,

        /// <summary>
        /// Corresponds to the six months retention policy.
        /// </summary>
        SixMonths = 4,

        /// <summary>
        /// Corresponds to other retention policy.
        /// </summary>
        Other = 5,
    }
}