namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class SharingRequestFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify SharingRequestFilterCriteria.BuildRequestString()")]
        [InlineData(true, true, "$filter=deleteAgentId eq 'id2' and ownerId eq 'id1'")]
        [InlineData(true, false, "$filter=ownerId eq 'id1'")]
        [InlineData(false, true, "$filter=deleteAgentId eq 'id2'")]
        [InlineData(false, false, "")]
        public void VerifySharingRequestFilterCriteria(bool hasOwnerId, bool hasDeleteAgentId, string value)
        {
            var filter = new SharingRequestFilterCriteria
            {
                OwnerId = hasOwnerId ? "id1" : null,
                DeleteAgentId = hasDeleteAgentId ? "id2" : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(value, requestString);
        }
    }
}