namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PafMapperTests
    {
        private readonly string variantRequestId;
        private readonly IEnumerable<AssetGroupVariant> assetGroupVariants;
        private readonly IEnumerable<VariantRelationship> variantRelationShips;
        private readonly PafMapper mapper;
        private readonly MappingProfile profile;
        private readonly VariantRequest variantRequest;
        private ExtendedVariantRequest extendedVariantRequest;

        public PafMapperTests()
        {
            this.assetGroupVariants = new List<AssetGroupVariant>()
            {
                new AssetGroupVariant()
                {
                    VariantId = "63b4e6fa-b8b8-4c56-a0ea-40e1751fddbd",
                    VariantName = "Test VariantName",
                    VariantState = "Requested",
                    VariantExpiryDate = DateTimeOffset.UtcNow,
                    EgrcId = "Test EgrcId",
                    EgrcName = "Test EgrcName",
                    DisableSignalFiltering = false
                },
                new AssetGroupVariant() { VariantId = "6be244c4-9b0c-416c-a39c-3b2d05b084de" }
            };
            this.variantRelationShips = new List<VariantRelationship>()
            {
                new VariantRelationship()
                {
                    AssetGroupId = "sdfsdw32406-bb7c-4555-a9f7-c8bcsdf5cfd7",
                    AssetQualifier = "AssetType=AzureSql;ServerName=testserver;DatabaseName=testdb;TableName=table"
                },
                new VariantRelationship() { AssetGroupId = "ceb55ac0-d60f-41c2-b8f7-0abd4ade6b8a" }
            };
            this.variantRequestId = "62bcd884-d66f-4c1f-9e98-74422629fb0a";
            this.variantRequest = new VariantRequest()
            {
                RequestedVariants = this.assetGroupVariants,
                VariantRelationships = this.variantRelationShips,
                Id = this.variantRequestId,
                GeneralContractorAlias = "gcalias",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
                ETag = "Test Etag",
                OwnerId = "84744b0a-d66f-4c1f-9e98-2262bcd8629f",
                OwnerName = "Test Owner",
                WorkItemUri = new Uri("https://dev.azure.com/nextgenpriv/NGP-Test/_workitems/edit/13678/"),
                AdditionalInformation = "Test AdditionalInformation"
            };
            this.profile = new MappingProfile();
            this.mapper = new PafMapper(this.profile);
        }

        [TestMethod]
        public void GetExtendedVariantRequestReturnsExtendedVariantRequestIdenticalToVariantRequestOnSuccess()
        {
            this.extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(this.variantRequest);
            VerifyVariantRequestMapping(this.variantRequest, this.extendedVariantRequest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfProfileIsNull()
        {
            new PafMapper(null);
        }

        [TestMethod]
        public void ConstructorWorks()
        {
            PafMapper mapper = new PafMapper(this.profile);
            Assert.IsNotNull(mapper);
        }

        private static void VerifyVariantRequestMapping(VariantRequest variantRequest, ExtendedVariantRequest extendedVariantRequest)
        {
            Assert.AreEqual(variantRequest.Id, extendedVariantRequest.Id);
            Assert.AreEqual(variantRequest.GeneralContractorAlias, extendedVariantRequest.GeneralContractorAlias);
            Assert.AreEqual(variantRequest.ETag, extendedVariantRequest.ETag);
            Assert.AreEqual(variantRequest.OwnerName, extendedVariantRequest.OwnerName);
            Assert.AreEqual(variantRequest.WorkItemUri.ToString(), extendedVariantRequest.WorkItemUri.ToString());
            Assert.AreEqual(variantRequest.CelaContactAlias, extendedVariantRequest.CelaContactAlias);
            Assert.AreEqual(variantRequest.AdditionalInformation, extendedVariantRequest.AdditionalInformation);
            Assert.AreEqual(variantRequest.OwnerId, extendedVariantRequest.OwnerId);
            Assert.AreEqual(variantRequest.RequesterAlias, extendedVariantRequest.RequesterAlias);
            VerifyVariants(variantRequest.RequestedVariants, extendedVariantRequest.RequestedVariants);
            VerifyRelationShips(variantRequest.VariantRelationships, extendedVariantRequest.VariantRelationships);
        }

        private static void VerifyVariants(IEnumerable<AssetGroupVariant> srcVariants, IEnumerable<ExtendedAssetGroupVariant> destVariants)
        {
            Assert.AreEqual(srcVariants.Count(), destVariants.Count());
            srcVariants.Zip(destVariants, (src, dest) =>
            {
                Assert.AreEqual(src.EgrcId, dest.EgrcId);
                Assert.AreEqual(src.EgrcId, dest.EgrcId);
                Assert.AreEqual(src.EgrcName, dest.EgrcName);
                Assert.IsTrue((src.VariantExpiryDate?.Equals(dest.VariantExpiryDate) ?? false) | (src.VariantExpiryDate == src.VariantExpiryDate));
                Assert.AreEqual(src.VariantId, dest.VariantId);
                Assert.AreEqual(src.VariantName, dest.VariantName);
                Assert.AreEqual(src.VariantState, dest.VariantState);
                Assert.AreEqual(src.DisableSignalFiltering, dest.DisableSignalFiltering);
                return true;
            });
        }

        private static void VerifyRelationShips(IEnumerable<VariantRelationship> srcRelation, IEnumerable<VariantRelationship> destRelation)
        {
                Assert.AreEqual(srcRelation.Count(), destRelation.Count());
                srcRelation.Zip(destRelation, (src, dest) =>
                {
                    Assert.AreEqual(src.AssetGroupId, dest.AssetGroupId);
                    Assert.AreEqual(src.AssetQualifier, dest.AssetQualifier);
                    return true;
                });
        }
    }
}
