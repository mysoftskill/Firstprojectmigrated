namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    /// <summary>
    /// Common properties for all filter criteria.
    /// </summary>
    public interface IFilterCriteria
    {
        /// <summary>
        /// Gets or sets the number of items to retrieve.
        /// </summary>
        int? Count { get; set; }

        /// <summary>
        /// Gets or sets the index from which to start retrieving items.
        /// </summary>
        int? Index { get; set; }

        /// <summary>
        /// Builds the http request string for this filter based on the filter's value and comparison type.
        /// </summary>
        /// <returns>The request string.</returns>
        string BuildRequestString();
    }

    /// <summary>
    /// Filter behavior for all filter criteria.
    /// </summary>
    /// <typeparam name="TEntity">The entity whose properties are used for filtering.</typeparam>
    public interface IFilterCriteria<TEntity> : IFilterCriteria
    {
    }

    /// <summary>
    /// Filter behavior for all filter criteria.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Builds the http request string for this filter based on the filter's value and comparison type.
        /// </summary>
        /// <param name="propertyName">The property name to filter on.</param>
        /// <returns>The request string.</returns>
        string BuildFilterString(string propertyName);
    }
}