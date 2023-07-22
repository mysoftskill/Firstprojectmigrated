using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Models.ServiceTree;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents parameters to create a data owner using Service Tree.
    /// </summary>
    public class ServiceTreeOwner : IEntityWithIcmInformation
    {
        /// <summary>
        /// Gets or sets data owner ID used for update.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Service Tree ID to use.
        /// </summary>
        public string ServiceTreeId { get; set; }

        /// <summary>
        /// Gets or sets the kind of ID that <see cref="ServiceTreeId"/> is.
        /// </summary>
        public ServiceTreeEntityKind ServiceTreeIdKind { get; set; } = ServiceTreeEntityKind.Service;

        /// <summary>
        /// Gets or sets IcM connector ID.
        /// </summary>
        public string IcmConnectorId { get; set; }

        /// <summary>
        /// Gets or sets write security groups. 
        /// </summary>
        public IEnumerable<string> WriteSecurityGroups { get; set; }

        /// <summary>
        /// Gets or sets tagging security groups. 
        /// </summary>
        public IEnumerable<string> TagSecurityGroups { get; set; }

        /// <summary>
        /// Gets or sets tagging application ids. 
        /// </summary>
        public IEnumerable<string> TagApplicationIds { get; set; }

        /// <summary>
        /// Gets or sets sharing request contacts.
        /// </summary>
        public IEnumerable<string> SharingRequestContacts { get; set; }
    }
}
