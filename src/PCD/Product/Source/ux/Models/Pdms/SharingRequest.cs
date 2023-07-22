using System.Collections.Generic;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a sharing request.
    /// </summary>
    public class SharingRequest
    {
        /// <summary>
        /// Gets or sets the request id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets owner id on the request
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the delete agent id on the request
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// Gets or sets the owner name on the request
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// Gets or sets the relationships associated to the request
        /// </summary>
        public IEnumerable<SharingRelationship> Relationships { get; set; }        
    }
}
