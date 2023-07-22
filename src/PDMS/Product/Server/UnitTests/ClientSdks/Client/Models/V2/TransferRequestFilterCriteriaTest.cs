namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Xunit;

    public class TransferRequestFilterCriteriaTest
    {
        [Theory(DisplayName = "Verify AssetGroupFilterCriteria.BuildRequestString()")]
        [InlineData(false, false, "")]
        [InlineData(true, false, "$filter=sourceOwnerId eq 'id1'")]
        [InlineData(false, true, "$filter=targetOwnerId eq 'id2'")]
        [InlineData(true, true, "$filter=targetOwnerId eq 'id2' and sourceOwnerId eq 'id1'")]
        public void VerifyAssetGroupFilterCriteria(bool hasSourceOwnerId, bool hasTargetOwnerId, string value)
        {
            var filter = new TransferRequestFilterCriteria
            {
                SourceOwnerId = hasSourceOwnerId ? "id1" : null,
                TargetOwnerId = hasTargetOwnerId ? "id2" : null
            };

            var requestString = filter.BuildRequestString();

            Assert.Equal(value, requestString);
        }
    }
}