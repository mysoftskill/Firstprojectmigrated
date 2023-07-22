namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// AssetGroup filter criteria used in Get operation.
    /// </summary>
    public class AssetGroupFilterCriteria : EntityFilterCriteria<AssetGroup>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupFilterCriteria"/> class.
        /// </summary>
        public AssetGroupFilterCriteria() : base()
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
        /// Gets or sets the export agent id to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string ExportAgentId { get; set; }

        /// <summary>
        /// Gets or sets the asset qualifier to search by. If this is not set, then it is not included in the query.
        /// This only supports Contains and Equals comparisons. The value must be an AssetQualifier as a string.
        /// </summary>
        public StringFilter Qualifier { get; set; }

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

            if (this.ExportAgentId != null)
            {
                requestString = new StringFilter(this.ExportAgentId, StringComparisonType.Equals).BuildFilterString("exportAgentId").And(requestString);
            }

            if (this.Qualifier != null)
            {
                requestString = this.Qualifier.BuildFilterString("qualifier").And(requestString);
            }

            return requestString;
        }
    }
}