namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VariantRequestPatchSerializerTests
    {
        private readonly VariantRequestWorkItem variantRequestWorkItem;

        private Guid variantRequestId;

        public VariantRequestPatchSerializerTests()
        {
            this.variantRequestId = new Guid("62bcd884-d66f-4c1f-9e98-74422629fb0a");
            this.variantRequestWorkItem = new VariantRequestWorkItem()
            {
                Variants = null,
                AssetGroups = null,
                WorkItemTitle = "Testing Title",
                WorkItemDescription = "Testing Description",
                VariantRequestId = this.variantRequestId.ToString(),
                GeneralContractorAlias = "gcalias",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
            };
        }

        [TestMethod]
        public void SerializesCreatePatchDocument()
        {
            var correctDocument = new Dictionary<string, string>()
            {
                { "/fields/System.Title", "Testing Title" },
                { "/fields/System.Description", "Testing Description" },
                { "/fields/System.State", "New" },
                { "/fields/Custom.ListofVariantsDescrip", "<div>Variant list is empty.<div>" },
                { "/fields/Custom.ListofAssetGroups", "<div>Asset group list is empty.<div>" },
                { "/fields/Custom.VariantRequestId", "62bcd884-d66f-4c1f-9e98-74422629fb0a" },
                { "/fields/Custom.GeneralContractorAlias", "gcalias" },
                { "/fields/Custom.CELAContactAlias", "celaalias" },
                { "/fields/Custom.RequesterAlias", "requesteralias" },
            };
            var correctCount = correctDocument.Count;

            VariantRequestPatchSerializer serializer = new VariantRequestPatchSerializer();
            var addPatchDocument = serializer.CreateVariantRequestPatchDocument(this.variantRequestWorkItem);
            Assert.AreEqual(addPatchDocument.Count, correctCount, $"There should be {correctCount} fields in the patch document entry");
            foreach (var operation in addPatchDocument)
            {
                Assert.AreEqual(correctDocument[operation.Path], operation.Value, $"The field {operation.Path} is invalid");
            }
        }

        [TestMethod]
        public void CreatesUpdatePatchDocument()
        {
            VariantRequestPatchSerializer serializer = new VariantRequestPatchSerializer();
            Dictionary<string, string> updateRequest = new Dictionary<string, string>()
            {
                { "System.State", "Approved" }
            };
            var correctDocument = new Dictionary<string, string>()
            {
                { "/fields/System.State", "Approved" }
            };
            var updateDocument = serializer.UpdateVariantRequestPatchDocument(updateRequest);
            foreach (var operation in updateDocument)
            {
                Assert.AreEqual(correctDocument[operation.Path], operation.Value);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UpdatePatchDocumentInvalidState()
        {
            VariantRequestPatchSerializer serializer = new VariantRequestPatchSerializer();
            Dictionary<string, string> updateRequest = new Dictionary<string, string>()
            {
                { "System.State", "False State" }
            };
            _ = serializer.UpdateVariantRequestPatchDocument(updateRequest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UpdatePatchDocumentInvalidField()
        {
            VariantRequestPatchSerializer serializer = new VariantRequestPatchSerializer();
            Dictionary<string, string> updateRequest = new Dictionary<string, string>()
            {
                { "Invalid Field", "Approved" }
            };
            _ = serializer.UpdateVariantRequestPatchDocument(updateRequest);
        }

        [TestMethod]
        public void AddListOfVariantRequests()
        {
            List<ExtendedAssetGroupVariant> variants = new List<ExtendedAssetGroupVariant>()
            {
                new ExtendedAssetGroupVariant() { VariantId = "63b4e6fa-b8b8-4c56-a0ea-40e1751fddbd" },
                new ExtendedAssetGroupVariant() { VariantId = "6be244c4-9b0c-416c-a39c-3b2d05b084de" }
            };
            List<VariantRelationship> assetGroups = new List<VariantRelationship>()
            {
                new VariantRelationship() { AssetGroupId = "c8ce4186-3bcb-44c7-bdfb-1e6b4476dfb5" },
                new VariantRelationship() { AssetGroupId = "ceb55ac0-d60f-41c2-b8f7-0abd4ade6b8a" }
            };

            var complexWorkItem = new VariantRequestWorkItem()
            {
                Variants = variants,
                AssetGroups = assetGroups,
                WorkItemTitle = "Testing Title",
                WorkItemDescription = "Testing Description",
                VariantRequestId = this.variantRequestId.ToString(),
                GeneralContractorAlias = "gcalias",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
            };
            var correctDocument = new Dictionary<string, string>()
            {
                { "/fields/System.Title", "Testing Title" },
                { "/fields/System.Description", "Testing Description" },
                { "/fields/System.State", "New" },
                { "/fields/Custom.VariantRequestId", "62bcd884-d66f-4c1f-9e98-74422629fb0a" },
                { "/fields/Custom.GeneralContractorAlias", "gcalias" },
                { "/fields/Custom.CELAContactAlias", "celaalias" },
                { "/fields/Custom.RequesterAlias", "requesteralias" },
            };

            VariantRequestPatchSerializer serializer = new VariantRequestPatchSerializer();
            var addPatchDocument = serializer.CreateVariantRequestPatchDocument(complexWorkItem);
            foreach (var operation in addPatchDocument)
            {
                if (operation.Path.EndsWith("ListofVariantsDescrip"))
                {
                    foreach (var variant in variants)
                    {
                        string operationValue = operation.Value.ToString();
                        Assert.IsTrue(operationValue.Contains(variant.VariantId), operationValue, $"The field {operation.Path} is invalid");
                    }
                }
                else if (operation.Path.EndsWith("ListofAssetGroups"))
                {
                    foreach (var assetGroup in assetGroups)
                    {
                        string operationValue = operation.Value.ToString();
                        Assert.IsTrue(operationValue.Contains(assetGroup.AssetGroupId), operationValue, $"The field {operation.Path} is invalid");
                    }
                }
                else
                {
                    Assert.AreEqual(correctDocument[operation.Path], operation.Value, $"The field {operation.Path} is invalid");
                }
            }
        }
    }
}
