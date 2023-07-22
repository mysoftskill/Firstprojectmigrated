namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing variant definition state.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<VariantDefinitionState>))]
    public enum VariantDefinitionState
    {
        /// <summary>
        /// Corresponds to the variant definitions Active state.
        /// </summary>
        Active = 0,

        /// <summary>
        /// Corresponds to the variant definition Closed state.
        /// </summary>
        Closed = 1,
    }
}
