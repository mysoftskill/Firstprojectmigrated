using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Search
{
    /// <summary>
    /// Search result that corresponds to a PDMS entity.
    /// </summary>
    public class SearchEntityResult : SearchResultBase
    {
        /// <summary>
        /// Gets or sets entity ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets entity name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets entity description. Can be null.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets ID of the entity's owner. Not set, if entity doesn't have an owner.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets entity's owner name.
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// Gets or sets agent Id.
        /// </summary>
        public string AgentId { get; set; }
    }
}
