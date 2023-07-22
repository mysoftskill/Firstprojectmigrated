namespace PCF.UnitTests.Pdms
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests PrivacyCommand 
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class PrivacyCommandTests : INeedDataBuilders
    {
        [Fact]
        public void AreClaimedVariantsValidReturnsTrueForValidClaim()
        {
            var variantInfo = this.AnAssetGroupVariantInfoDocument().With(x => x.DataTypes, new string[0]).Build();

            var assetGroupDocument = this.AnAssetGroupInfoDocument()
                .With(x => x.VariantInfosAppliedByAgents, new List<AssetGroupVariantInfoDocument> { variantInfo }).Build();

            var assetGroup = new AssetGroupInfo(assetGroupDocument, false);

            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.True(command.AreClaimedVariantsValid(new[] { variantInfo.VariantId.Value }, assetGroup));
        }

        [Fact]
        public void AreClaimedVariantsValidReturnsTrueForValidClaimInPcfVariants()
        {
            var variantInfo = this.AnAssetGroupVariantInfoDocument().With(x => x.DataTypes, new string[0]).Build();
            var assetGroupDocument = this.AnAssetGroupInfoDocument()
                .With(x => x.VariantInfosAppliedByPcf, new List<AssetGroupVariantInfoDocument> { variantInfo }).Build();

            var assetGroup = new AssetGroupInfo(assetGroupDocument, false);

            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.True(command.AreClaimedVariantsValid(new[] { variantInfo.VariantId.Value }, assetGroup));
        }

        [Fact]
        public void AreClaimedVariantsValidReturnsTrueForAtleastOneValidClaim()
        {
            var variantInfo = this.AnAssetGroupVariantInfoDocument().With(x => x.DataTypes, new string[0]).Build();

            var assetGroupDocument = this.AnAssetGroupInfoDocument()
                .With(x => x.VariantInfosAppliedByAgents, new List<AssetGroupVariantInfoDocument> { variantInfo }).Build();

            var assetGroup = new AssetGroupInfo(assetGroupDocument, false);

            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.True(command.AreClaimedVariantsValid(new[] { variantInfo.VariantId.Value, Guid.NewGuid().ToString() }, assetGroup));
        }

        [Fact]
        public void AreClaimedVariantsValidReturnsTrueFoNoClaims()
        {
            var variantInfo = this.AnAssetGroupVariantInfoDocument().Build();

            var assetGroupDocument = this.AnAssetGroupInfoDocument()
                .With(x => x.VariantInfosAppliedByAgents, new List<AssetGroupVariantInfoDocument> { variantInfo }).Build();

            var assetGroup = new AssetGroupInfo(assetGroupDocument, false);
            
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.True(command.AreClaimedVariantsValid(new string[0], assetGroup));
        }

        [Fact]
        public void AreClaimedVariantsValidReturnsFalseIfNotFound()
        {
            var variantInfo = this.AnAssetGroupVariantInfoDocument().Build();

            var assetGroupDocument = this.AnAssetGroupInfoDocument()
                .With(x => x.VariantInfosAppliedByAgents, new List<AssetGroupVariantInfoDocument> { variantInfo }).Build();

            var assetGroup = new AssetGroupInfo(assetGroupDocument, false);

            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.False(command.AreClaimedVariantsValid(new[] {  Guid.NewGuid().ToString() }, assetGroup));
        }

        [Fact]
        public async Task IsVerifierValidAsyncReturnsTrueIfValidVerifierAsync()
        {
            var validationServiceMock = this.AMockOf<IValidationService>();
            
            var assetGroup = this.AnAssetGroupInfo();
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);
            
            Assert.True(await command.IsVerifierValidAsync(validationServiceMock.Object));
            validationServiceMock.Verify(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IsVerifierValidAsyncReturnsFalseIfKeyDiscoveryException()
        {
            var validationServiceMock = this.AMockOf<IValidationService>();
            validationServiceMock.Setup(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<KeyDiscoveryException>();
            var assetGroup = this.AnAssetGroupInfo();
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.False(await command.IsVerifierValidAsync(validationServiceMock.Object));
        }

        [Fact]
        public async Task IsVerifierValidAsyncReturnsFalseIfInvalidPrivacyCommandException()
        {
            var validationServiceMock = this.AMockOf<IValidationService>();
            validationServiceMock.Setup(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<InvalidPrivacyCommandException>();
            var assetGroup = this.AnAssetGroupInfo();
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.False(await command.IsVerifierValidAsync(validationServiceMock.Object));
        }

        [Fact]
        public async Task IsVerifierValidAsyncReturnsFalseIfOperationCanceledException()
        {
            var validationServiceMock = this.AMockOf<IValidationService>();
            validationServiceMock.Setup(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();
            var assetGroup = this.AnAssetGroupInfo();
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            Assert.False(await command.IsVerifierValidAsync(validationServiceMock.Object));
        }

        [Fact]
        public async Task IsVerifierValidAsyncThrowsIfFileNotFoundException()
        {
            var validationServiceMock = this.AMockOf<IValidationService>();
            validationServiceMock.Setup(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<FileNotFoundException>();
            var assetGroup = this.AnAssetGroupInfo();
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                PcfTestCapability.Delete,
                PdmsSubjectType.MSAUser,
                new[] { PcfTestDataType.BrowsingHistory },
                assetGroup,
                null);

            await Assert.ThrowsAsync<FileNotFoundException>(() => command.IsVerifierValidAsync(validationServiceMock.Object));
        }
    }
}
