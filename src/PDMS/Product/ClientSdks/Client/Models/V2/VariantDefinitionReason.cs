namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing the variant definition close reason.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<VariantDefinitionReason>))]
    public enum VariantDefinitionReason
    {
        /// <summary>
        /// Corresponds to a variant definition that is still active.
        /// </summary>
        None = 0,

        /// <summary>
        /// Corresponds to the reason that the variant definition was closed intentionally.
        /// </summary>
        Intentional = 1,

        /// <summary>
        /// Corresponds to the reason that the variant definition expired.
        /// </summary>
        Expired = 2,
    }
}
