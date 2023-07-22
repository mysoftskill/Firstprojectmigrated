using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Models.ServiceTree;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents data owner.
    /// </summary>
    public class DataOwner : IEntityWithIcmInformation
    {
        /// <summary>
        /// Gets or sets data owner ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets data owner name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets data owner description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets IcM connector ID.
        /// </summary>
        public string IcmConnectorId { get; set; }

        /// <summary>
        /// Gets or sets alert contacts.
        /// </summary>
        public IEnumerable<string> AlertContacts { get; set; }

        /// <summary>
        /// Gets or sets announcement contacts.
        /// </summary>
        public IEnumerable<string> AnnouncementContacts { get; set; }

        /// <summary>
        /// Gets or sets sharing request contacts.
        /// </summary>
        public IEnumerable<string> SharingRequestContacts { get; set; }

        /// <summary>
        /// Gets or sets asset groups.
        /// </summary>
        public IEnumerable<AssetGroup> AssetGroups { get; set; }

        /// <summary>
        /// Gets or sets data agents.
        /// </summary>
        public IEnumerable<DataAgent> DataAgents { get; set; }

        /// <summary>
        /// Gets or sets write security groups.
        /// </summary>
        public IEnumerable<string> WriteSecurityGroups { get; set; }

        /// <summary>
        /// The security group ids used by DataGrid to authorize tagging.
        /// </summary>
        public IEnumerable<string> TagSecurityGroups { get; set; }

        /// <summary>
        /// The application ids used by DataGrid to authorize tagging.
        /// </summary>
        public IEnumerable<string> TagApplicationIds { get; set; }

        /// <summary>
        /// Gets or sets Service Tree information.
        /// Creating a DataOwner with this field requires WriteSecurityGroups to not be null, and all other fields to be null.
        /// PDMS will populate some fields after creation, and those fields must remain intact during any update, with the exception of WriteSecurityGroups. 
        /// </summary>
        public ServiceTreeEntityDetails ServiceTree { get; set; }

        /// <summary>
        /// Gets or sets boolean for if the owner has any pending transfer requests.
        /// </summary>
        public bool HasPendingTransferRequests { get; set; }
    }
}
