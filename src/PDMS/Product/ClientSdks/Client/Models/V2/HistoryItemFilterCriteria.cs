namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// History item filter criteria used in Get operation.
    /// </summary>
    public class HistoryItemFilterCriteria : PagingCriteria, IFilterCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItemFilterCriteria"/> class.
        /// </summary>
        public HistoryItemFilterCriteria()
        {
        }

        /// <summary>
        /// Gets or sets the entity id to search by. If this is not set, then it is not included in the query.
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets the start from filter.
        /// </summary>
        public DateTimeOffset? EntityUpdatedAfter { get; set; }

        /// <summary>
        /// Gets or sets the end at filter.
        /// </summary>
        public DateTimeOffset? EntityUpdatedBefore { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildFilterString()
        {
            var requestString = base.BuildFilterString();

            if (this.EntityId != null)
            {
                requestString = new StringFilter(this.EntityId, StringComparisonType.Equals).BuildFilterString("entity/id").And(requestString);
            }

            if (this.EntityUpdatedAfter != null)
            {
                requestString = new DateTimeOffsetFilter(this.EntityUpdatedAfter.Value, NumberComparisonType.GreaterThanOrEquals).BuildFilterString("entity/trackingDetails/updatedOn").And(requestString);
            }

            if (this.EntityUpdatedBefore != null)
            {
                requestString = new DateTimeOffsetFilter(this.EntityUpdatedBefore.Value, NumberComparisonType.LessThanOrEquals).BuildFilterString("entity/trackingDetails/updatedOn").And(requestString);
            }

            return requestString;
        }
    }
}