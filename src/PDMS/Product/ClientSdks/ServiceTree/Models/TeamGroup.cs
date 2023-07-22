namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;

    /// <summary>
    /// The model for team groups. There are more properties in service tree than what is listed below.
    /// </summary>
    public class TeamGroup : ServiceTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamGroup" /> class.
        /// </summary>
        public TeamGroup() : base(ServiceTreeLevel.TeamGroup)
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
    }
}