namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// VariantRequest filter criteria used in Get operation.
    /// </summary>
    public class VariantRequestFilterCriteria : EntityFilterCriteria<VariantRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestFilterCriteria"/> class.
        /// </summary>
        public VariantRequestFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the owner id to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string OwnerId { get; set; }

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

            return requestString;
        }
    }
}