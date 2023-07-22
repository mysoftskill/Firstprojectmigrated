namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class DeleteAgentFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify DeleteAgentFilterCriteria.BuildRequestString()")]
        [InlineData("Name", StringComparisonType.Contains, true, false, "$filter=ownerId eq 'id' and contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, true, false, "$filter=ownerId eq 'id' and name eq 'Name'")]
        [InlineData("Name", StringComparisonType.Contains, false, false, "$filter=contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, false, false, "$filter=name eq 'Name'")]
        [InlineData(null, StringComparisonType.Contains, true, false, "$filter=ownerId eq 'id'")]
        [InlineData(null, StringComparisonType.Equals, false, false, "")]
        [InlineData("Name", StringComparisonType.Contains, true, true, "$filter=sharingEnabled eq true and ownerId eq 'id' and contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, true, true, "$filter=sharingEnabled eq true and ownerId eq 'id' and name eq 'Name'")]
        [InlineData("Name", StringComparisonType.Contains, false, true, "$filter=sharingEnabled eq true and contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, false, true, "$filter=sharingEnabled eq true and name eq 'Name'")]
        [InlineData(null, StringComparisonType.Contains, true, true, "$filter=sharingEnabled eq true and ownerId eq 'id'")]
        [InlineData(null, StringComparisonType.Equals, false, true, "$filter=sharingEnabled eq true")]
        public void VerifyDeleteAgentFilterCriteria(
            string name, 
            StringComparisonType comparisonType, 
            bool hasOwnerId, 
            bool hasSharingEnabled,
            string expectedResult)
        {
            var filter = new DeleteAgentFilterCriteria
            {
                Name = name == null ? null : new StringFilter(name, comparisonType),
                OwnerId = hasOwnerId ? "id" : null,
                SharingEnabled = hasSharingEnabled ? true : (bool?)null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }
    }
}