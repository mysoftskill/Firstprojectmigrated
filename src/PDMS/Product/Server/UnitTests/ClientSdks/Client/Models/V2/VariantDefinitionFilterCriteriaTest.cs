namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class VariantDefinitionFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify VariantDefinitionFilterCriteria.BuildRequestString()")]
        [InlineData(true, "Name", StringComparisonType.Contains, false, "", "$filter=contains(name,'Name')")]
        [InlineData(true, "Name", StringComparisonType.Equals, false, "", "$filter=name eq 'Name'")]
        [InlineData(true, "Name", StringComparisonType.Contains, true, "Active", "$filter=state eq 'Active' and contains(name,'Name')")]
        [InlineData(true, "Name", StringComparisonType.Equals, true, "Closed", "$filter=state eq 'Closed' and name eq 'Name'")]
        [InlineData(false, "", StringComparisonType.Contains, true, "Active", "$filter=state eq 'Active'")]
        [InlineData(false, "", StringComparisonType.Equals, true, "Closed", "$filter=state eq 'Closed'")]
        public void VerifyVariantDefinitionFilterCriteria(bool hasName, string name, StringComparisonType comparisonType, bool hasState, string stateValue, string expectedResult)
        {
            var filter = new VariantDefinitionFilterCriteria
            {
                Name = hasName ? new StringFilter(name, comparisonType) : null,
                State = hasState ? stateValue : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }

        [Fact(DisplayName = "Verify VariantDefinitionFilterCriteria.BuildRequestString() with Or clause.")]
        public void VerifyAssetGroupFilterCriteriaWithOr()
        {
            var filter = new VariantDefinitionFilterCriteria
            {
                Name = new StringFilter("Name", StringComparisonType.Equals),
                Or = new VariantDefinitionFilterCriteria
                {
                    State = "Active"
                }
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal("$filter=(state eq 'Active') or (name eq 'Name')", requestString);
        }
    }
}