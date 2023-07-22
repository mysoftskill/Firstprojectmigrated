namespace Microsoft.PrivacyServices.DataManagement.Client.Filters.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    using Xunit;

    public class DateTimeOffsetFilterTest
    {
        // TODO: Remove?   Not really a valid test
        ////[Theory(DisplayName = "Verify all guards for DateTimeOffsetFilter")]
        ////[InlineData(null)]
        ////public void VerifyDateTimeOffsetFilterGuards(DateTimeOffset value)
        ////{
        ////    Assert.Throws<ArgumentNullException>(() => new DateTimeOffsetFilter(value, NumberComparisonType.GreaterThanOrEquals));
        ////}

        [Theory(DisplayName = "Verify DateTimeOffsetFilter.BuildRequestString()")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.GreaterThan, "entity/trackingDetails/updatedOn gt {0}")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.GreaterThanOrEquals, "entity/trackingDetails/updatedOn ge {0}")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.LessThan, "entity/trackingDetails/updatedOn lt {0}")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.LessThanOrEquals, "entity/trackingDetails/updatedOn le {0}")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.NotEquals, "entity/trackingDetails/updatedOn ne {0}")]
        [InlineData("entity/trackingDetails/updatedOn", NumberComparisonType.Equals, "entity/trackingDetails/updatedOn eq {0}")]
        public void VerifyStringFilter(string propertyName, NumberComparisonType comparisionType, string expectedResultFormat)
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;

            DateTimeOffsetFilter filter = new DateTimeOffsetFilter(dateTimeOffset, comparisionType);

            var requestString = filter.BuildFilterString(propertyName);

            Assert.Equal(string.Format(expectedResultFormat, dateTimeOffset.UtcDateTime.ToString("o")), requestString);
        }

        [Fact(DisplayName = "When an invalid enum type is provided to DateTimeOffsetFilter.BuildRequestString(), then throw ArgumentOutOfRangeException")]
        public void VerifyStringFilterException()
        {
            var filter = new DateTimeOffsetFilter(DateTimeOffset.UtcNow, (NumberComparisonType)short.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => filter.BuildFilterString("a"));
        }
    }
}