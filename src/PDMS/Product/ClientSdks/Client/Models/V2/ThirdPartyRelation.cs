namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Enum describing inventory third party relation.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(EnumTolerantConverter<ThirdPartyRelation>))]
    public enum ThirdPartyRelation
    {
        /// <summary>
        /// Corresponds to no third party relation of referenced entities.
        /// </summary>
        None = 0,

        /// <summary>
        /// Corresponds to the internal only inventory.
        /// </summary>
        Internal = 1 << 0,

        /// <summary>
        /// Corresponds to the inventory that is sent to third parties.
        /// </summary>
        SentTo = 1 << 1,

        /// <summary>
        /// Corresponds to the inventory that is received from third parties.
        /// </summary>
        ReceivedFrom = 1 << 2,
    }
}