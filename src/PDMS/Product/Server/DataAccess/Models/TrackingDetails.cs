namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains the server generated tracking information. This data is updated as part of every change operation.
    /// </summary>
    public class TrackingDetails
    {
        /// <summary>
        /// Gets or sets the version. Tracks the changes of this entity. Increments every time a change is applied.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the username of the account that registered the entity.
        /// </summary>
        [JsonProperty(PropertyName = "createdBy")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        [JsonProperty(PropertyName = "createdOn")]
        public DateTimeOffset CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the username of the account that last updated the entity.
        /// </summary>
        [JsonProperty(PropertyName = "updatedBy")]
        public string UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modified date time.
        /// </summary>
        [JsonProperty(PropertyName = "updatedOn")]
        public DateTimeOffset UpdatedOn { get; set; }
        
        /// <summary>
        /// Gets or sets the time at which the entity was lasted modified through the egress pipeline.
        /// </summary>
        [JsonProperty(PropertyName = "egressedOn")]
        public DateTimeOffset EgressedOn { get; set; }
    }
}