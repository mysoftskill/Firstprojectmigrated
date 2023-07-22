namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines contact and security information for associated entities.
    /// </summary>
    public class DataOwner
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name. This is a human readable value for display purposes.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description. This value is an optional value. Use this to provide any additional information that can help explain the entity.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the security group ids to use when authorizing changes to the associated entities.
        /// These should be the object ids (Guids) from Azure Active Directory.
        /// </summary>
        [JsonProperty(PropertyName = "writeSecurityGroups")]
        public IEnumerable<Guid> WriteSecurityGroups { get; set; }

        /// <summary>
        /// Gets or sets the security group ids to use when authorizing tagging in DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "tagSecurityGroups")]
        public IEnumerable<Guid> TagSecurityGroups { get; set; }

        /// <summary>
        /// Gets or sets the app ids to use when authorizing tagging in DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "tagApplicationIds")]
        public IEnumerable<Guid> TagApplicationIds { get; set; }

        /// <summary>
        /// Gets or sets the service tree meta data for the data owner. 
        /// This information is returned from service tree.
        /// This value is only returned if it is explicitly requested in a $select statement.
        /// </summary>
        [JsonProperty(PropertyName = "serviceTree")]
        public ServiceTree ServiceTree { get; set; }

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