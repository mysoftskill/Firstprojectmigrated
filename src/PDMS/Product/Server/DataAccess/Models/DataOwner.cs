namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines contact and security information for associated entities.
    /// </summary>
    public class DataOwner : NamedEntity
    {
        /// <summary>
        /// The email addresses to use when there is an issue with the associated agents.
        /// </summary>
        [JsonProperty(PropertyName = "alertContacts")]
        public IEnumerable<string> AlertContacts { get; set; }

        /// <summary>
        /// The email addresses to use when announcements are made. If no values are provided, then the alert contacts will be used instead.
        /// </summary>
        [JsonProperty(PropertyName = "announcementContacts")]
        public IEnumerable<string> AnnouncementContacts { get; set; }

        /// <summary>
        /// The email addresses to use when there is a request to link to any data agents linked to this owner.
        /// </summary>
        [JsonProperty(PropertyName = "sharingRequestContacts")]
        public IEnumerable<string> SharingRequestContacts { get; set; }

        /// <summary>
        /// The security group ids to use when authorizing changes to the associated agents.
        /// These should be the object ids (guids) from Azure Active Directory.
        /// </summary>
        [JsonProperty(PropertyName = "writeSecurityGroups")]
        public IEnumerable<string> WriteSecurityGroups { get; set; }

        /// <summary>
        /// The security group ids used by DataGrid to authorize tagging.
        /// These should be the object ids (guids) from Azure Active Directory.
        /// If this is empty, then DataGrid falls back to the write security groups.
        /// </summary>
        [JsonProperty(PropertyName = "tagSecurityGroups")]
        public IEnumerable<string> TagSecurityGroups { get; set; }

        /// <summary>
        /// The security group ids containing app ids used by DataGrid to authorize tagging.
        /// If this is empty, then DataGrid falls back to the tag security groups or write security groups.
        /// </summary>
        [JsonProperty(PropertyName = "tagApplicationIds")]
        public IEnumerable<string> TagApplicationIds { get; set; }

        /// <summary>
        /// The data agents. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "dataAgents")]
        public IEnumerable<DataAgent> DataAgents { get; set; }

        /// <summary>
        /// The asset groups. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroups")]
        public IEnumerable<AssetGroup> AssetGroups { get; set; }

        /// <summary>
        /// The service tree meta data for the data owner. 
        /// This information is returned from service tree.
        /// This value is only returned if it is explicitly requested in a $select statement.
        /// </summary>
        [JsonProperty(PropertyName = "serviceTree")]
        public ServiceTree ServiceTree { get; set; }

        /// <summary>
        /// ICM meta data.
        /// </summary>
        [JsonProperty(PropertyName = "icm")]
        public Icm Icm { get; set; }

        /// <summary>
        /// Indicates whether this data owner has initiated any transfer request.
        /// </summary>
        [JsonProperty(PropertyName = "hasInitiatedTransferRequests")]
        public bool HasInitiatedTransferRequests { get; set; }

        /// <summary>
        /// Indicates whether this data owner has any pending transfer request.
        /// </summary>
        [JsonProperty(PropertyName = "hasPendingTransferRequests")]
        public bool HasPendingTransferRequests { get; set; }
    }
}