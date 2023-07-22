namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    using System;

    /// <summary>
    /// Filter criteria for string.
    /// All string operations (contains, equals etc.) are done in case insensitive way.
    /// </summary>
    public class StringFilter : BaseFilter<string, StringComparisonType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFilter"/> class.
        /// </summary>
        /// <param name="value">String value to use in filter.</param>
        /// <param name="comparisonType">Comparison type.</param>
        public StringFilter(string value, StringComparisonType comparisonType)
            : base(value, comparisonType)
        {
            if (string.IsNullOrWhiteSpace(value) && comparisonType != StringComparisonType.Equals)
            {
                throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Builds the http request string for this filter based on the filter's value and comparison type.
        /// </summary>
        /// <param name="propertyName">The property name to filter on.</param>
        /// <returns>The request string.</returns>
        public override string BuildFilterString(string propertyName)
        {
            switch (this.ComparisonType)
            {
                case StringComparisonType.Contains:
                    return string.Format("contains({0},'{1}')", propertyName, this.Value);

                case StringComparisonType.Equals:
                    if (this.Value == null)
                    {
                        return string.Format("{0} eq null", propertyName);
                    }
                    else
                    {
                        return string.Format("{0} eq '{1}'", propertyName, this.Value);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(this.ComparisonType), this.ComparisonType, "Unrecognized enumeration value.");
            }
        }
    }
}