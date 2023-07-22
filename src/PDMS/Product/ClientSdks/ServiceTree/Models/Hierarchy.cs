namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents and item in the hierarchy.
    /// </summary>
    public class Hierarchy
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the hierarchy level for this entity.
        /// </summary>
        [JsonProperty("NodeType")]
        public virtual ServiceTreeLevel Level { get; set; }
    }
}