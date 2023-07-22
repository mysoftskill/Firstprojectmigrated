namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    /// <summary>
    /// An empty filter criteria, which provides just enough properties to enable client side paging support.
    /// </summary>
    public class PagingCriteria : IFilterCriteria
    {
        /// <summary>
        /// Gets or sets the number of items to retrieve.
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets the index from which to start retrieving items.
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// Builds the http request string for this filter based on the filter's value and comparison type.
        /// </summary>
        /// <returns>The request string.</returns>
        public string BuildRequestString()
        {
            var value = this.BuildFilterString();

            if (!string.IsNullOrEmpty(value))
            {
                value = $"$filter={value}";
            }

            var pagingString = this.BuildPagingString();

            if (!string.IsNullOrEmpty(pagingString))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value += "&";
                }

                value += pagingString;
            }

            return value;
        }

        /// <summary>
        /// Builds the filter string for the request.
        /// </summary>
        /// <returns>The filter string.</returns>
        protected virtual string BuildFilterString()
        {
            return string.Empty;
        }

        private string BuildPagingString()
        {
            var value = string.Empty;

            if (this.Count.HasValue)
            {
                value += $"$top={this.Count}";
            }

            if (this.Index.HasValue)
            {
                if (this.Count.HasValue)
                {
                    value += "&";
                }

                value += $"$skip={this.Index}";
            }

            return value;
        }
    }
}