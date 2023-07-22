namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing data inventory documentation type.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<DocumentationType>))]
    public enum DocumentationType
    {
        /// <summary>
        /// Corresponds to the data flow diagram documentation type.
        /// </summary>
        DataFlowDiagram = 1,
    }
}