namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;

    /// <summary>
    /// The model for services. There are more properties in service tree than what is listed below.
    /// </summary>
    public class Service : ServiceTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Service" /> class.
        /// </summary>
        public Service() : base(ServiceTreeLevel.Service)
        {
        }

        /// <summary>
        /// Gets or sets the service group id.
        /// </summary>
        public Guid? ServiceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the service group name.
        /// </summary>
        public string ServiceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the team group id.
        /// </summary>
        public Guid? TeamGroupId { get; set; }

        /// <summary>
        /// Gets or sets the service group name.
        /// </summary>
        public string TeamGroupName { get; set; }    
        
        /// <summary>
        /// Gets or sets the admin security groups.
        /// </summary>
        public string AdminSecurityGroups { get; set; }
    }
}