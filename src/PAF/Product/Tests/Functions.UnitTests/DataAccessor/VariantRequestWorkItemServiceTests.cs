namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.DataAccessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.WebApi.Patch;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VariantRequestWorkItemServiceTests
    {
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IFunctionConfiguration> configurationMock;
        private readonly Mock<IAdoClientWrapper> adoClientWrapperMock;
        private readonly Mock<IVariantRequestPatchSerializer> patchSerializerMock;
        private readonly ExtendedVariantRequest variantRequest;

        public VariantRequestWorkItemServiceTests()
        {
            this.loggerMock = new Mock<ILogger>();
            this.configurationMock = new Mock<IFunctionConfiguration>();
            this.adoClientWrapperMock = new Mock<IAdoClientWrapper>();
            this.patchSerializerMock = new Mock<IVariantRequestPatchSerializer>();

            var variant = new ExtendedAssetGroupVariant()
            {
                VariantId = Guid.NewGuid().ToString(),
                VariantName = "Variant",
                EgrcId = "EgrcId",
                EgrcName = "EgrcName"
            };

            var assetGroup = new VariantRelationship()
            {
                AssetGroupId = Guid.NewGuid().ToString(),
                AssetQualifier = "AssetQualfier"
            };

            this.variantRequest = new ExtendedVariantRequest()
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = Guid.NewGuid().ToString(),
                OwnerName = "ownerName",
                GeneralContractorAlias = "gcalias",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
                VariantRelationships = new List<VariantRelationship>() { assetGroup },
                RequestedVariants = new List<ExtendedAssetGroupVariant>() { variant }
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfConfigurationIsNull()
        {
            _ = new VariantRequestWorkItemService(null, this.adoClientWrapperMock.Object, this.loggerMock.Object, this.patchSerializerMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfHttpClientWrapperIsNull()
        {
            _ = new VariantRequestWorkItemService(this.configurationMock.Object, null, this.loggerMock.Object, this.patchSerializerMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfLoggerIsNull()
        {
            var service = new VariantRequestWorkItemService(this.configurationMock.Object, this.adoClientWrapperMock.Object, null, this.patchSerializerMock.Object);
            Assert.IsNotNull(service);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfPatchSerializerIsNull()
        {
            _ = new VariantRequestWorkItemService(this.configurationMock.Object, this.adoClientWrapperMock.Object, this.loggerMock.Object, null);
        }

        [TestMethod]
        public void ConstructorWorks()
        {
            var service = new VariantRequestWorkItemService(this.configurationMock.Object, this.adoClientWrapperMock.Object, this.loggerMock.Object, this.patchSerializerMock.Object);
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public async Task CreateWorkItemSuccessAsync()
        {
            var patchDocument = new Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "fakepath",
                    Value = "fakevalue"
                }
            };

            WorkItem shouldReturnWorkItem = new WorkItem()
            {
                Id = 1234,
                Url = "test.com"
            };

            // Is Any is used here as BuildVariantRequestWorkItem and BuildComment are tested elsewhere
            this.patchSerializerMock.Setup(psMock => psMock.CreateVariantRequestPatchDocument(It.IsAny<VariantRequestWorkItem>())).Returns(patchDocument).Verifiable();

            this.patchSerializerMock.Setup(psMock => psMock.UpdateVariantRequestPatchDocument(It.IsAny<Dictionary<string, string>>())).Returns(patchDocument).Verifiable();

            this.adoClientWrapperMock.Setup(adoMock => adoMock.CreateWorkItemAsync(patchDocument, "VariantRequest", It.IsAny<string>())).Returns(Task.FromResult(shouldReturnWorkItem)).Verifiable();
            this.adoClientWrapperMock.Setup(adoMock => adoMock.UpdateWorkItemAsync(patchDocument, 1234)).Returns(Task.FromResult(shouldReturnWorkItem)).Verifiable();

            var workItemService = new VariantRequestWorkItemService(this.configurationMock.Object, this.adoClientWrapperMock.Object, this.loggerMock.Object, this.patchSerializerMock.Object);

            var workItem = await workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            Assert.AreEqual(shouldReturnWorkItem.Id, workItem.Id);

            this.adoClientWrapperMock.VerifyAll();
            this.patchSerializerMock.VerifyAll();
        }
    }
}
