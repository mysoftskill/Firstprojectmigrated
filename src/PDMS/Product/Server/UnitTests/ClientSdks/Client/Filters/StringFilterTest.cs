namespace Microsoft.PrivacyServices.DataManagement.Client.Filters.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    using Xunit;

    public class StringFilterTest
    {
        [Theory(DisplayName = "Verify all guards for StringFilter")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void VerifyStringFilterGuards(string value)
        {
            Assert.Throws<ArgumentNullException>(() => new StringFilter(value, StringComparisonType.Contains));
        }

        [Theory(DisplayName = "Verify StringFilter.BuildRequestString()")]
        [InlineData("friendlyName", "FriendlyName", StringComparisonType.Contains, "contains(friendlyName,'FriendlyName')")]
        [InlineData("friendlyName", "FriendlyName", StringComparisonType.Equals, "friendlyName eq 'FriendlyName'")]
        [InlineData("friendlyName", null, StringComparisonType.Equals, "friendlyName eq null")]
        public void VerifyStringFilter(string propertyName, string value, StringComparisonType comparisionType, string expectedResult)
        {
            StringFilter filter = new StringFilter(value, comparisionType);

            var requestString = filter.BuildFilterString(propertyName);

            Assert.Equal(expectedResult, requestString);
        }

        [Fact(DisplayName = "When an invalid enum type is provided to StringFilter.BuildRequestString(), then throw ArgumentOutOfRangeException")]
        public void VerifyStringFilterException()
        {
            var filter = new StringFilter("a", (StringComparisonType)short.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => filter.BuildFilterString("a"));
        }
    }
}