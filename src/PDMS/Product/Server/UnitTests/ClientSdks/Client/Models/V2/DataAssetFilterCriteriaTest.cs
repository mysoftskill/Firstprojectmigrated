namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{ 
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class DataAssetFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify DataAssetFilterCriteria.BuildRequestString() with paging values only")]
        [InlineData(null, null, "")]
        [InlineData(1, 2, "$top=1&$skip=2")]
        [InlineData(1, null, "$top=1")]
        [InlineData(null, 2, "$skip=2")]
        public void VerifyDataAssetPagingCriteria(int? count, int? index, string expectedResult)
        {
            var filter = new DataAssetFilterCriteria
            {
                Count = count,
                Index = index
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(expectedResult, requestString);
        }
    }
}