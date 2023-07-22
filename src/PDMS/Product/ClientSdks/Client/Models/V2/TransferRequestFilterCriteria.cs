namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// TransferRequest filter criteria used in Get operation.
    /// </summary>
    public class TransferRequestFilterCriteria : EntityFilterCriteria<TransferRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransferRequestFilterCriteria"/> class.
        /// </summary>
        public TransferRequestFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the source owner id to search by. 
        /// </summary>
        public string SourceOwnerId { get; set; }

        /// <summary>
        /// Gets or sets the target owner id to search by. 
        /// </summary>
        public string TargetOwnerId { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildExpression()
        {
            var requestString = base.BuildExpression();

            if (this.SourceOwnerId != null)
            {
                requestString = new StringFilter(this.SourceOwnerId, StringComparisonType.Equals).BuildFilterString("sourceOwnerId").And(requestString);
            }

            if (this.TargetOwnerId != null)
            {
                requestString = new StringFilter(this.TargetOwnerId, StringComparisonType.Equals).BuildFilterString("targetOwnerId").And(requestString);
            }

            return requestString;
        }
    }
}