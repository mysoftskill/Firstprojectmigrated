namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class DataAgentFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify DataAgentFilterCriteria.BuildRequestString()")]
        [InlineData("Name", StringComparisonType.Contains, true, "$filter=ownerId eq 'id' and contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, true, "$filter=ownerId eq 'id' and name eq 'Name'")]
        [InlineData("Name", StringComparisonType.Contains, false, "$filter=contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, false, "$filter=name eq 'Name'")]
        [InlineData(null, StringComparisonType.Contains, true, "$filter=ownerId eq 'id'")]
        [InlineData(null, StringComparisonType.Equals, true, "$filter=ownerId eq 'id'")]
        public void VerifyDataAgentFilterCriteria(string name, StringComparisonType comparisonType, bool hasOwnerId, string expectedResult)
        {
            var filter = new DataAgentFilterCriteria
            {
                Name = name == null ? null : new StringFilter(name, comparisonType),
                OwnerId = hasOwnerId ? "id" : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }
    }
}