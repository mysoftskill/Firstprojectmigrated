namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class InventoryFilterCritereiaTest
    {
        [Theory(DisplayName = "Verify InventoryFilterCriteria.BuildRequestString()")]
        [InlineData("Name", StringComparisonType.Contains, "$filter=contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, "$filter=name eq 'Name'")]
        public void VerifyInventoryFilterCriteria(string name, StringComparisonType comparisonType, string expectedResult)
        {
            var filter = new InventoryFilterCriteria
            {
                Name = new StringFilter(name, comparisonType)
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }
    }
}