namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing write action type.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<WriteAction>))]
    public enum WriteAction
    {
        /// <summary>
        /// Corresponds to entity create operation.
        /// </summary>
        Create = 1,

        /// <summary>
        /// Corresponds to entity update operation.
        /// </summary>
        Update = 2,

        /// <summary>
        /// Corresponds to entity soft delete operation.
        /// </summary>
        SoftDelete = 3,

        /// <summary>
        /// Corresponds to entity hard delete operation.
        /// </summary>
        HardDelete = 4
    }
}
