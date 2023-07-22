namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// DeleteAgent filter criteria used in Get operation.
    /// </summary>
    public class DeleteAgentFilterCriteria : DataAgentFilterCriteria<DeleteAgent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAgentFilterCriteria"/> class.
        /// </summary>
        public DeleteAgentFilterCriteria() : base()
        {
        }

        /// <summary>
        /// Gets or sets the sharing enabled value to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public bool? SharingEnabled { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildExpression()
        {
            var requestString = base.BuildExpression();

            if (this.SharingEnabled != null)
            {
                requestString = $"sharingEnabled eq {this.SharingEnabled.Value.ToString().ToLower()}".And(requestString);
            }

            return requestString;
        }
    }
}