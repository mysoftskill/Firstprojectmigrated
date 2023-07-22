namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class HistoryItemFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify HistoryFilterCriteria.BuildRequestString()")]
        [InlineData(true, true, true, "$filter=entity/trackingDetails/updatedOn le {0} and entity/trackingDetails/updatedOn ge {0} and entity/id eq 'id'")]
        [InlineData(false, true, true, "$filter=entity/trackingDetails/updatedOn le {0} and entity/trackingDetails/updatedOn ge {0}")]
        [InlineData(true, false, true, "$filter=entity/trackingDetails/updatedOn le {0} and entity/id eq 'id'")]
        [InlineData(true, false, false, "$filter=entity/id eq 'id'")]
        [InlineData(false, true, false, "$filter=entity/trackingDetails/updatedOn ge {0}")]
        [InlineData(false, false, true, "$filter=entity/trackingDetails/updatedOn le {0}")]
        [InlineData(false, false, false, "")]
        public void VerifyHistoryItemFilterCriteria(bool hasEntityId, bool hasEntityUpdatedAfter, bool hasEntityUpdatedBefore, string expectedResultFormat)
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;

            var filter = new HistoryItemFilterCriteria
            {
                EntityId = hasEntityId ? "id" : null,
                EntityUpdatedAfter = hasEntityUpdatedAfter ? dateTimeOffset : (DateTimeOffset?)null,
                EntityUpdatedBefore = hasEntityUpdatedBefore ? dateTimeOffset : (DateTimeOffset?)null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(string.Format(expectedResultFormat, dateTimeOffset.UtcDateTime.ToString("o")), requestString);
        }
    }
}