namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class DataOwnerFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify DataOwnerFilterCriteria.BuildRequestString()")]
        [InlineData("Name", StringComparisonType.Contains, "$filter=contains(name,'Name')")]
        [InlineData("Name", StringComparisonType.Equals, "$filter=name eq 'Name'")]
        public void VerifyDataOwnerFilterCriteria(string name, StringComparisonType comparisonType, string expectedResult)
        {
            var filter = new DataOwnerFilterCriteria
            {
                Name = new StringFilter(name, comparisonType)
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }

        [Theory(DisplayName = "Verify DataOwnerFilterCriteria.BuildRequestString()")]
        [InlineData("DivisionId", "Value", StringComparisonType.Equals, "$filter=serviceTree/divisionId eq 'Value'")]
        [InlineData("DivisionName", "Value", StringComparisonType.Equals, "$filter=serviceTree/divisionName eq 'Value'")]
        [InlineData("OrganizationId", "Value", StringComparisonType.Equals, "$filter=serviceTree/organizationId eq 'Value'")]
        [InlineData("OrganizationName", "Value", StringComparisonType.Equals, "$filter=serviceTree/organizationName eq 'Value'")]
        [InlineData("ServiceGroupId", "Value", StringComparisonType.Equals, "$filter=serviceTree/serviceGroupId eq 'Value'")]
        [InlineData("ServiceGroupName", "Value", StringComparisonType.Equals, "$filter=serviceTree/serviceGroupName eq 'Value'")]
        [InlineData("TeamGroupId", "Value", StringComparisonType.Equals, "$filter=serviceTree/teamGroupId eq 'Value'")]
        [InlineData("TeamGroupName", "Value", StringComparisonType.Equals, "$filter=serviceTree/teamGroupName eq 'Value'")]
        [InlineData("ServiceId", "Value", StringComparisonType.Equals, "$filter=serviceTree/serviceId eq 'Value'")]
        [InlineData("ServiceId", null, StringComparisonType.Equals, "$filter=serviceTree/serviceId eq null")]
        [InlineData("ServiceName", "Value", StringComparisonType.Equals, "$filter=serviceTree/serviceName eq 'Value'")]
        public void VerifyDataOwnerFilterCriteriaForServiceTree(string propertyName, string propertyValue, StringComparisonType comparisonType, string expectedResult)
        {
            var filter = new DataOwnerFilterCriteria();
            filter.ServiceTree = new ServiceTreeFilterCriteria();
            filter.ServiceTree.GetType().GetProperty(propertyName).SetValue(filter.ServiceTree, new StringFilter(propertyValue, comparisonType));

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }

        [Theory(DisplayName = "Verify DataOwnerFilterCriteria.BuildRequestString() with paging values only")]
        [InlineData(null, null, "")]
        [InlineData(1, 2, "$top=1&$skip=2")]
        [InlineData(1, null, "$top=1")]
        [InlineData(null, 2, "$skip=2")]
        public void VerifyDataOwnerPagingCriteria(int? count, int? index, string expectedResult)
        {
            var filter = new DataOwnerFilterCriteria
            {
                Count = count,
                Index = index
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }

        [Fact(DisplayName = "Verify DataOwnerFilterCriteria.BuildRequestString() with filter and paging values")]
        public void VerifyDataOwnerPagingCriteriaWithFilterValues()
        {
            var filter = new DataOwnerFilterCriteria
            {
                Count = 1,
                Index = 2,
                Name = new StringFilter("test", StringComparisonType.Equals)
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal("$filter=name eq 'test'&$top=1&$skip=2", requestString);
        }
    }
}