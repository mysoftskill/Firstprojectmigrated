namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    
    /// <summary>
    /// Determines an asset grouping based on a single storage type. 
    /// </summary>
    public class AssetGroup
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        
        /// <summary>
        /// Gets or sets the qualifier as it appears in storage.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        public IDictionary<string, string> QualifierParts { get; set; }

        /// <summary>
        /// Gets or sets the list of variants on this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "variants")]
        public IEnumerable<AssetGroupVariant> Variants { get; set; }

        /// <summary>
        /// Gets or sets the id of the associated asset group owner.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the delete agent that contains this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "deleteAgentId")]
        public Guid? DeleteAgentId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the export delete agent that contains this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "exportAgentId")]
        public Guid? ExportAgentId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this asset group is blocked from broader asset groups inheritance if they have variant links.
        /// </summary>
        [JsonProperty(PropertyName = "isVariantsInheritanceBlocked")]
        public bool IsVariantsInheritanceBlocked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this asset group is blocked from broader asset groups inheritance if they have delete agent links.
        /// </summary>
        [JsonProperty(PropertyName = "isDeleteAgentInheritanceBlocked")]
        public bool IsDeleteAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this asset group is blocked from broader asset groups inheritance if they have export agent links.
        /// </summary>
        [JsonProperty(PropertyName = "isExportAgentInheritanceBlocked")]
        public bool IsExportAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is deleted.
        /// </summary>
        [JsonProperty(PropertyName = "isDeleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value that provides sequencing information.
        /// </summary>
        [JsonProperty(PropertyName = "_lsn")]
        public long LogicalSequenceNumber { get; set; }
    }
}