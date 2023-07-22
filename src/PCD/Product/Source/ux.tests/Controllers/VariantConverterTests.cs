using System.Collections.Generic;
using System.Linq;
using Microsoft.PrivacyServices.UX.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Tests.Controllers
{
    [TestClass]
    public class VariantConverterTests
    {
        Fixture fixture = new Fixture();

        [TestMethod]
        public void ToVariantRequestModelWithNames_PopulateNames_When_VariantIdsArePresent()
        {
            var requestedVariant1 = fixture.Build<PdmsApiModelsV2.AssetGroupVariant>().With(x => x.VariantId, "1").Create();
            var requestedVariant2 = fixture.Build<PdmsApiModelsV2.AssetGroupVariant>().With(x => x.VariantId, "2").Create();
            var variantNames = new Dictionary<string, string>()
            {
                { "1", "variant1" },
                { "2", "variant2" },
            };

            var request = fixture.Build<PdmsApiModelsV2.VariantRequest>().With(vr => vr.VariantRelationships, new List<PdmsApiModelsV2.VariantRelationship>()).Create();
            request.RequestedVariants = new List<PdmsApiModelsV2.AssetGroupVariant>() { requestedVariant1, requestedVariant2 };

            var variantRequest = VariantConverters.ToVariantRequestModelWithNames(request, variantNames);

            Assert.AreEqual(variantRequest.RequestedVariants.ElementAt(0).VariantName, "variant1");
            Assert.AreEqual(variantRequest.RequestedVariants.ElementAt(1).VariantName, "variant2");
        }

        [TestMethod]
        public void ToVariantRequestModelWithNames_PopulateNames_When_VariantIdsAreNotPresent()
        {
            var requestedVariant1 = fixture.Build<PdmsApiModelsV2.AssetGroupVariant>().With(x => x.VariantId, "1").Create();
            var requestedVariant2 = fixture.Build<PdmsApiModelsV2.AssetGroupVariant>().With(x => x.VariantId, "2").Create();
            var variantNames = new Dictionary<string, string>()
            {
                { "1", "variant1" },
            };

            var request = fixture.Build<PdmsApiModelsV2.VariantRequest>().With(vr => vr.VariantRelationships, new List<PdmsApiModelsV2.VariantRelationship>()).Create();
            request.RequestedVariants = new List<PdmsApiModelsV2.AssetGroupVariant>() { requestedVariant1, requestedVariant2 };

            var variantRequest = VariantConverters.ToVariantRequestModelWithNames(request, variantNames);

            Assert.AreEqual(variantRequest.RequestedVariants.ElementAt(0).VariantName, "variant1");
            Assert.AreEqual(variantRequest.RequestedVariants.ElementAt(1).VariantName, "2");
        }
    }
}
