namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    using System;

    /// <summary>
    /// Filter criteria for date time offset.
    /// </summary>
    public class DateTimeOffsetFilter : BaseFilter<DateTimeOffset, NumberComparisonType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetFilter"/> class.
        /// </summary>
        /// <param name="value">Date time offset value to use in filter.</param>
        /// <param name="comparisonType">Comparison type.</param>
        public DateTimeOffsetFilter(DateTimeOffset value, NumberComparisonType comparisonType)
            : base(value, comparisonType)
        {
            if (value == null || value == default)
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
            // Date time offset format required by OData standard.
            var dateTimeOffsetOutput = this.Value.UtcDateTime.ToString("o");

            switch (this.ComparisonType)
            {
                case NumberComparisonType.GreaterThan:
                    return string.Format("{0} gt {1}", propertyName, dateTimeOffsetOutput);
                case NumberComparisonType.GreaterThanOrEquals:
                    return string.Format("{0} ge {1}", propertyName, dateTimeOffsetOutput);
                case NumberComparisonType.LessThan:
                    return string.Format("{0} lt {1}", propertyName, dateTimeOffsetOutput);
                case NumberComparisonType.LessThanOrEquals:
                    return string.Format("{0} le {1}", propertyName, dateTimeOffsetOutput);
                case NumberComparisonType.NotEquals:
                    return string.Format("{0} ne {1}", propertyName, dateTimeOffsetOutput);
                case NumberComparisonType.Equals:
                    return string.Format("{0} eq {1}", propertyName, dateTimeOffsetOutput);
                default:
                    throw new ArgumentOutOfRangeException(nameof(this.ComparisonType), this.ComparisonType, "Unrecognized enumeration value.");
            }
        }
    }
}