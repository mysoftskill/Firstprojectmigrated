namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    /// <summary>
    /// Base filter behavior for all filters.
    /// </summary>
    /// <typeparam name="TValue">The filter's value type.</typeparam>
    /// <typeparam name="TComparisonType">The filter's comparison type.</typeparam>
    public abstract class BaseFilter<TValue, TComparisonType> : IFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseFilter{TValue, TComparisonType}"/> class.
        /// </summary>
        /// <param name="value">Value to use in the filter.</param>
        /// <param name="comparisonType">Comparison type.</param>
        protected BaseFilter(TValue value, TComparisonType comparisonType)
        {
            this.Value = value;
            this.ComparisonType = comparisonType;
        }

        /// <summary>
        /// Gets the value for comparison.
        /// </summary>
        public TValue Value { get; private set; }

        /// <summary>
        /// Gets the comparison type.
        /// </summary>
        public TComparisonType ComparisonType { get; private set; }

        /// <summary>
        /// Builds the http request string for this filter based on the filter's value and comparison type.
        /// </summary>
        /// <param name="propertyName">The property name to filter on.</param>
        /// <returns>The request string.</returns>
        public abstract string BuildFilterString(string propertyName);
    }
}