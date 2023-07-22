namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// The level in the hierarchy.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<ServiceTreeLevel>))]
    public enum ServiceTreeLevel
    {
        /// <summary>
        /// Represents a division.
        /// </summary>
        Division,

        /// <summary>
        /// Represents an organization.
        /// </summary>
        Organization,

        /// <summary>
        /// Represents a service group.
        /// </summary>
        ServiceGroup,

        /// <summary>
        /// Represents a team group.
        /// </summary>
        TeamGroup,

        /// <summary>
        /// Represents a service.
        /// </summary>
        Service
    }

    /// <summary>
    /// The model for service groups. There are more properties in service tree than what is listed below.
    /// </summary>
    public abstract class ServiceTreeNode : Hierarchy
    {
        private ServiceTreeLevel level;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeNode" /> class.
        /// </summary>
        /// <param name="level">The level for this entity.</param>
        protected ServiceTreeNode(ServiceTreeLevel level)
        {
            this.level = level;
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the admin user names.
        /// </summary>
        public IEnumerable<string> AdminUserNames { get; set; }

        /// <summary>
        /// Gets or sets the division id.
        /// </summary>
        public Guid? DivisionId { get; set; }

        /// <summary>
        /// Gets or sets the division name.
        /// </summary>
        public string DivisionName { get; set; }

        /// <summary>
        /// Gets or sets the organization id.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets the hierarchy level for this entity.
        /// </summary>
        public override ServiceTreeLevel Level
        {
            get
            {
                return this.level;
            }

            set
            {
                // This is readonly.
            }
        }
    }
}