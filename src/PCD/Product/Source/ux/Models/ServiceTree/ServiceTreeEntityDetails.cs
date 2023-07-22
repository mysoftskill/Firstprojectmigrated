using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Models.ServiceTree
{
    /// <summary>
    /// Defines service tree entity details.
    /// </summary>
    public class ServiceTreeEntityDetails
    {
        /// <summary>
        /// Gets or sets service tree entity ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets service tree ID kind.
        /// </summary>
        public ServiceTreeEntityKind Kind { get; set; }

        /// <summary>
        /// Gets or sets service tree entity name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets service tree entity description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a list of admin aliases of the service tree entity.
        /// </summary>
        public IEnumerable<string> ServiceAdmins { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the division ID.
        /// </summary>
        public string DivisionId { get; set; }
    }
}
