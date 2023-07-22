using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.Cms;
using Microsoft.PrivacyServices.UX.Core.Cms.Model;
using Microsoft.PrivacyServices.UX.Core.Cms.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.Services.CompassService.Client;
using Microsoft.Windows.Services.CompassService.Client.Model;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Core.Cms
{
    [TestClass]
    public class CmsLoaderTests
    {
        Mock<ICmsKeyParser> cmsKeyParserMock;
        Mock<ICmsClient> cmsClientMock;
        Mock<IClientProviderAccessor<ICmsClientProvider>> cmsClientProviderAccessorMock;
        Mock<ICmsClientProvider> cmsClientProviderMock;
        CmsLoader cmsLoader;

        [TestInitialize]
        public void Initialize()
        {
            cmsKeyParserMock = new Mock<ICmsKeyParser>();
            cmsClientMock = new Mock<ICmsClient>();
            cmsClientProviderMock = new Mock<ICmsClientProvider>();
            cmsClientProviderAccessorMock = new Mock<IClientProviderAccessor<ICmsClientProvider>>();

            cmsClientProviderMock.Setup(cp => cp.Instance).Returns(cmsClientMock.Object);
            cmsClientProviderAccessorMock.Setup(a => a.ProviderInstance).Returns(cmsClientProviderMock.Object);

            cmsLoader = new CmsLoader(cmsClientProviderAccessorMock.Object, cmsKeyParserMock.Object);
        }

        [TestMethod]
        public async Task CmsLoader_LoadsKnownContent()
        {

            await VerifyLoadsKnownContentOf<Page>("page");
            await VerifyLoadsKnownContentOf<Component>("component");
        }

        private async Task VerifyLoadsKnownContentOf<TCmsContent>(string typeName) where TCmsContent : class, IBaseCompassType, new()
        {
            var cmsKey = new CmsKey
            {
                CmsId = $"{typeName}.test-content", 
                AreaName = "test-area",
            };

            var parsedCmsKey = new ParsedCmsKey
            {
                CmsLocation = "test-area/test-content",
                CmsTypeName = typeName
            };

            var cultureCode = "en-US";
            TCmsContent content = Mock.Of<TCmsContent>();

            cmsKeyParserMock.Setup(c => c.Parse(cmsKey)).Returns(parsedCmsKey);
            cmsClientMock.Setup(c => c.GetContentItemAsync<TCmsContent>(parsedCmsKey.CmsLocation, cultureCode, null)).ReturnsAsync(content);

            var result = await cmsLoader.LoadAsync(cmsKey, cultureCode);

            Assert.AreEqual(content, result);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task CmsLoader_DoesNotLoadUnknownContent()
        {
            var cmsKey = new CmsKey
            {
                CmsId = "mysterious.test-content",
                AreaName = "test-area",
            };

            var parsedCmsKey = new ParsedCmsKey
            {
                CmsLocation = "test-area/test-content",
                CmsTypeName = "mysterious"
            };

            var cultureCode = "en-US";

            cmsKeyParserMock.Setup(c => c.Parse(cmsKey)).Returns(parsedCmsKey);

            var result = await cmsLoader.LoadAsync(cmsKey, cultureCode);
        }

        [TestCleanup]
        public void Cleanup()
        {
            cmsKeyParserMock.VerifyAll();
            cmsClientMock.VerifyAll();
        }
    }
}
