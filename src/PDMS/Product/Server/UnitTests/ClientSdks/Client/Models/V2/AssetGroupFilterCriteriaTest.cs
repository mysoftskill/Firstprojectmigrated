namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class AssetGroupFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify AssetGroupFilterCriteria.BuildRequestString()")]
        [InlineData(true, true, false, "$filter=deleteAgentId eq 'id2' and ownerId eq 'id1'")]
        [InlineData(true, false, false, "$filter=ownerId eq 'id1'")]
        [InlineData(false, true, false, "$filter=deleteAgentId eq 'id2'")]
        [InlineData(false, false, false, "")]
        [InlineData(true, true, true, "$filter=exportAgentId eq 'id3' and deleteAgentId eq 'id2' and ownerId eq 'id1'")]
        [InlineData(true, false, true, "$filter=exportAgentId eq 'id3' and ownerId eq 'id1'")]
        [InlineData(false, true, true, "$filter=exportAgentId eq 'id3' and deleteAgentId eq 'id2'")]
        [InlineData(false, false, true, "$filter=exportAgentId eq 'id3'")]
        public void VerifyAssetGroupFilterCriteria(bool hasOwnerId, bool hasDeleteAgentId, bool hasExportAgentId, string value)
        {
            var filter = new AssetGroupFilterCriteria
            {
                OwnerId = hasOwnerId ? "id1" : null,
                DeleteAgentId = hasDeleteAgentId ? "id2" : null,
                ExportAgentId = hasExportAgentId ? "id3" : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(value, requestString);
        }

        [Fact(DisplayName = "Verify AssetGroupFilterCriteria.BuildRequestString() with Or clause.")]
        public void VerifyAssetGroupFilterCriteriaWithOr()
        {
            var filter = new AssetGroupFilterCriteria
            {
                OwnerId = "id1",
                DeleteAgentId = "id2",
                Or = new AssetGroupFilterCriteria
                {
                    OwnerId = "id1",
                    ExportAgentId = "id2",
                    Or = new AssetGroupFilterCriteria
                    {
                        OwnerId = "id3"
                    }
                }
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal("$filter=((ownerId eq 'id3') or (exportAgentId eq 'id2' and ownerId eq 'id1')) or (deleteAgentId eq 'id2' and ownerId eq 'id1')", requestString);
        }
    }
}