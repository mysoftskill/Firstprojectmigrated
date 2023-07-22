namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// SharingRequest filter criteria used in Get operation.
    /// </summary>
    public class SharingRequestFilterCriteria : EntityFilterCriteria<SharingRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharingRequestFilterCriteria"/> class.
        /// </summary>
        public SharingRequestFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the owner id to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the delete agent id to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string DeleteAgentId { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildExpression()
        {
            var requestString = base.BuildExpression();

            if (this.OwnerId != null)
            {
                requestString = new StringFilter(this.OwnerId, StringComparisonType.Equals).BuildFilterString("ownerId").And(requestString);
            }

            if (this.DeleteAgentId != null)
            {
                requestString = new StringFilter(this.DeleteAgentId, StringComparisonType.Equals).BuildFilterString("deleteAgentId").And(requestString);
            }

            return requestString;
        }
    }
}