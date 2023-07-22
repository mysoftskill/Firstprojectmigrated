namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// DataOwner filter criteria used in Get operation.
    /// </summary>
    public class DataOwnerFilterCriteria : NamedEntityFilterCriteria<DataOwner>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerFilterCriteria"/> class.
        /// </summary>
        public DataOwnerFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the filter for the service tree property.
        /// </summary>
        public ServiceTreeFilterCriteria ServiceTree { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildExpression()
        {
            var requestString = base.BuildExpression();

            if (this.ServiceTree != null)
            {
                requestString = this.ServiceTree.BuildExpression().And(requestString);
            }

            return requestString;
        }
    }
}