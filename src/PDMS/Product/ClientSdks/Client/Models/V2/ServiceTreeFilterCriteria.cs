namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// A filter for service tree properties.
    /// </summary>
    public class ServiceTreeFilterCriteria
    {
        /// <summary>
        /// Gets or sets the filter for the division id property.
        /// </summary>
        public StringFilter DivisionId { get; set; }

        /// <summary>
        /// Gets or sets the filter for the division name property.
        /// </summary>
        public StringFilter DivisionName { get; set; }

        /// <summary>
        /// Gets or sets the filter for the organization id property.
        /// </summary>
        public StringFilter OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the filter for the organization name property.
        /// </summary>
        public StringFilter OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the filter for the service group id property.
        /// </summary>
        public StringFilter ServiceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the filter for the service group name property.
        /// </summary>
        public StringFilter ServiceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the filter for the team group id property.
        /// </summary>
        public StringFilter TeamGroupId { get; set; }

        /// <summary>
        /// Gets or sets the filter for the team group name property.
        /// </summary>
        public StringFilter TeamGroupName { get; set; }

        /// <summary>
        /// Gets or sets the filter for the service id property.
        /// </summary>
        public StringFilter ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the filter for the service name property.
        /// </summary>
        public StringFilter ServiceName { get; set; }

        /// <summary>
        /// Converts the filter criteria into an expression tree.
        /// </summary>
        /// <returns>The expression.</returns>
        internal string BuildExpression()
        {
            var expression = string.Empty;

            if (this.DivisionId != null)
            {
                expression = this.DivisionId.BuildFilterString("serviceTree/divisionId").And(expression);
            }

            if (this.DivisionName != null)
            {
                expression = this.DivisionName.BuildFilterString("serviceTree/divisionName").And(expression);
            }

            if (this.OrganizationId != null)
            {
                expression = this.OrganizationId.BuildFilterString("serviceTree/organizationId").And(expression);
            }

            if (this.OrganizationName != null)
            {
                expression = this.OrganizationName.BuildFilterString("serviceTree/organizationName").And(expression);
            }

            if (this.ServiceGroupId != null)
            {
                expression = this.ServiceGroupId.BuildFilterString("serviceTree/serviceGroupId").And(expression);
            }

            if (this.ServiceGroupName != null)
            {
                expression = this.ServiceGroupName.BuildFilterString("serviceTree/serviceGroupName").And(expression);
            }

            if (this.TeamGroupId != null)
            {
                expression = this.TeamGroupId.BuildFilterString("serviceTree/teamGroupId").And(expression);
            }

            if (this.TeamGroupName != null)
            {
                expression = this.TeamGroupName.BuildFilterString("serviceTree/teamGroupName").And(expression);
            }

            if (this.ServiceId != null)
            {
                expression = this.ServiceId.BuildFilterString("serviceTree/serviceId").And(expression);
            }

            if (this.ServiceName != null)
            {
                expression = this.ServiceName.BuildFilterString("serviceTree/serviceName").And(expression);
            }

            return expression;
        }
    }
}