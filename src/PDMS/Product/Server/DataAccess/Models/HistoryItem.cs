namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;

    /// <summary>
    /// The tracking history item.
    /// </summary>
    public class HistoryItem
    {
        /// <summary>
        /// Gets or sets the unique Id for this entity.
        /// This information is service generated. 
        /// Setting or modifying this value will result in an error.
        /// </summary>
        [Key]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets ETag for the entity. 
        /// This information is service generated. 
        /// Setting or modifying this value will result in an error.
        /// This property must match the stored value for change operations to succeed.
        /// If the value has changed, then an error will occur.
        /// </summary>
        [JsonProperty(PropertyName = "eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// The tracked history version of entity.
        /// </summary>
        [JsonProperty(PropertyName = "entity")]
        [JsonConverter(typeof(HistoryItemEntityConverter))]
        public Entity Entity { get; set; }

        /// <summary>
        /// The write action type for this history item. Create, Update or Delete.
        /// </summary>
        [JsonProperty(PropertyName = "writeAction")]
        public WriteAction WriteAction { get; set; }

        /// <summary>
        /// A unique id for all history items created during the same storage call.
        /// </summary>
        [JsonProperty(PropertyName = "transactionId")]
        public string TransactionId { get; set; }
    }
}