namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing asset group variant state.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [JsonConverter(typeof(EnumTolerantConverter<VariantState>))]
    public enum VariantState
    {
        /// <summary>
        /// Corresponds to the requested variant state.
        /// </summary>
        Requested = 1,

        /// <summary>
        /// Corresponds to the approved variant state.
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Corresponds to the deprecated variant state.
        /// </summary>
        Deprecated = 3,

        /// <summary>
        /// Corresponds to the rejected variant state.
        /// </summary>
        Rejected = 4,
    }
}