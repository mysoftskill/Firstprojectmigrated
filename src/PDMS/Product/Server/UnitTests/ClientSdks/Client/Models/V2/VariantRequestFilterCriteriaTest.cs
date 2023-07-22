namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class VariantRequestFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify VariantRequestFilterCriteria.BuildRequestString()")]
        [InlineData(true, "$filter=ownerId eq 'id1'")]
        [InlineData(false, "")]
        public void VerifyVariantRequestFilterCriteria(bool hasOwnerId, string value)
        {
            var filter = new VariantRequestFilterCriteria
            {
                OwnerId = hasOwnerId ? "id1" : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(value, requestString);
        }
    }
}