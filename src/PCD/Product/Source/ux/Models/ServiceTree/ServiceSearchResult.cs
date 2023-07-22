using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Models.ServiceTree
{
    /// <summary>
    /// Defines service tree entity result in a search.
    /// </summary>
    public class ServiceSearchResult
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
    }
}
