using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.PrivacyServices.UX.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Tests.Controllers
{
    [TestClass]
    public class ApiControllerTests
    {
        [TestMethod]
        public void ApiController_CorrectAttributes_Class()
        {
            Assert.IsTrue(Attributes.ClassHas<ApiController>(typeof(AuthorizeAttribute)));
            Assert.IsTrue(Attributes.ClassHas<ApiController, AuthorizeAttribute>(attribute => "Api" == attribute.Policy));
        }

        [TestMethod]
        public void ApiController_ConvertIcmInformation_NoNewIcmConfig_ResetsConfig()
        {
            var newConfig = Mock.Of<PdmsModels.IEntityWithIcmInformation>();
            var initialIcmConfig = new PdmsApiModelsV2.Icm();

            newConfig.IcmConnectorId = null;
            Assert.IsNull(ApiController.ConvertIcmInformation(newConfig, null));
            Assert.IsNull(ApiController.ConvertIcmInformation(newConfig, initialIcmConfig));

            newConfig.IcmConnectorId = string.Empty;
            Assert.IsNull(ApiController.ConvertIcmInformation(newConfig, null));
            Assert.IsNull(ApiController.ConvertIcmInformation(newConfig, initialIcmConfig));
        }

        [TestMethod]
        public void ApiController_ConvertIcmInformation_InitialIcmConfigMatchesNew_PreservesExistingConfig()
        {
            var newConfig = CreateNewIcmConfig();
            var initialIcmConfig = new PdmsApiModelsV2.Icm
            {
                ConnectorId = Guid.Parse(newConfig.IcmConnectorId),
                Source = PdmsApiModelsV2.IcmSource.ServiceTree,
                TenantId = 12345
            };

            var convertedIcmConfig = ApiController.ConvertIcmInformation(newConfig, initialIcmConfig);
            Assert.IsNotNull(convertedIcmConfig);
            Assert.AreEqual(initialIcmConfig.ConnectorId, convertedIcmConfig.ConnectorId);
            Assert.AreEqual(PdmsApiModelsV2.IcmSource.ServiceTree, convertedIcmConfig.Source);
            Assert.AreEqual(12345, convertedIcmConfig.TenantId);
        }

        [TestMethod]
        public void ApiController_ConvertIcmInformation_NoInitialIcmConfig_AppliesNewConfig()
        {
            var newConfig = CreateNewIcmConfig();

            var convertedIcmConfig = ApiController.ConvertIcmInformation(newConfig, initialIcmConfig: null);
            Assert.IsNotNull(convertedIcmConfig);
            Assert.AreEqual(newConfig.IcmConnectorId, convertedIcmConfig.ConnectorId.ToString());
            Assert.AreEqual(PdmsApiModelsV2.IcmSource.Manual, convertedIcmConfig.Source);
            Assert.AreEqual(0, convertedIcmConfig.TenantId);
        }

        [TestMethod]
        public void ApiController_ConvertIcmInformation_InitialIcmConfigDoesNotMatchNew_AppliesNewConfig()
        {
            var newConfig = CreateNewIcmConfig();
            var initialIcmConfig = new PdmsApiModelsV2.Icm
            {
                ConnectorId = Guid.NewGuid(),
                Source = PdmsApiModelsV2.IcmSource.ServiceTree,
                TenantId = 12345
            };

            var convertedIcmConfig = ApiController.ConvertIcmInformation(newConfig, initialIcmConfig);
            Assert.IsNotNull(convertedIcmConfig);
            Assert.AreEqual(Guid.Parse(newConfig.IcmConnectorId), convertedIcmConfig.ConnectorId);
            Assert.AreEqual(PdmsApiModelsV2.IcmSource.Manual, convertedIcmConfig.Source);
            Assert.AreEqual(0, convertedIcmConfig.TenantId);
        }

        private static PdmsModels.IEntityWithIcmInformation CreateNewIcmConfig()
        {
            var newConfig = Mock.Of<PdmsModels.IEntityWithIcmInformation>();
            newConfig.IcmConnectorId = Guid.NewGuid().ToString();

            return newConfig;
        }
    }
}
