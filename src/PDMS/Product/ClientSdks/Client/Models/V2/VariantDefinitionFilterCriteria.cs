namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// Variant definition filter criteria used in Get operation.
    /// </summary>
    public class VariantDefinitionFilterCriteria : NamedEntityFilterCriteria<VariantDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantDefinitionFilterCriteria"/> class.
        /// </summary>
        public VariantDefinitionFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the state to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the ownerId to search by. If this is not set, then it is not included in the query.
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

            if (this.State != null)
            {
                requestString = new StringFilter(this.State, StringComparisonType.Equals).BuildFilterString("state").And(requestString);
            }

            return requestString;
        }
    }
}