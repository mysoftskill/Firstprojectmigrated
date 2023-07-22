using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents an incident
    /// </summary>
    public class Incident
    {
        /// <summary>
        /// Gets or sets the id on the incident
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets routing information
        /// </summary>
        public RouteData Routing { get; set; }

        /// <summary>
        /// Gets or sets the severity level on the incident
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// Gets or sets the title of the incident
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body/description of the incident
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the keywords associated with the incident
        /// </summary>
        public string Keywords { get; set; }
    }

    /// <summary>
    /// Represents route data
    /// </summary>
    public class RouteData
    {
        /// <summary>
        /// Gets or sets the owner id
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the agent id
        /// </summary>
        public Guid? AgentId { get; set; }

        /// <summary>
        /// Gets or sets the asset group id
        /// </summary>
        public Guid? AssetGroupId { get; set; }
    }
}
