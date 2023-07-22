using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Core.Cms.Model;
using Microsoft.PrivacyServices.UX.Core.Cms.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Core.Cms
{
    [TestClass]
    public class CmsServiceTests
    {
        Mock<ICmsLoader> cmsLoaderMock;
        CmsService cmsService;

        [TestInitialize]
        public void Initialize()
        {
            cmsLoaderMock = new Mock<ICmsLoader>();
            cmsService = new CmsService(cmsLoaderMock.Object);
        }

        [TestMethod]
        public async Task GetCmsContentItem_Works()
        {
            var cmsKey = new CmsKey()
            {
                CmsId = "page.test-content",
                AreaName = "test-area"
            };
            var cultureCode = "en-US";
            var content = Mock.Of<Page>();

            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey, cultureCode)).ReturnsAsync(content);
            var result = await cmsService.GetCmsContentItemAsync(cmsKey, cultureCode);

            Assert.AreEqual(content, result);
        }

        [TestMethod]
        public async Task GetCmsContentItems_Works()
        {
            var cmsKey1 = new CmsKey()
            {
                CmsId = "page.test-content1",
                AreaName = "test-area"
            };
            var cmsKey2 = new CmsKey()
            {
                CmsId = "page.test-content2",
                AreaName = "test-area"
            };

            var cultureCode = "en-US";
            var content1 = Mock.Of<Page>();
            var content2 = Mock.Of<Page>();

            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey1, cultureCode)).ReturnsAsync(content1);
            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey2, cultureCode)).ReturnsAsync(content2);
            var result = await cmsService.GetCmsContentItemsAsync(new CmsKey[] { cmsKey1, cmsKey2 } , cultureCode);

            Assert.AreEqual(result.Keys.Count, 2);
            Assert.AreEqual(result.Keys.ElementAt(0), cmsKey1.CompositeKey);
            Assert.AreEqual(result.Keys.ElementAt(1), cmsKey2.CompositeKey);

            Assert.AreEqual(result.Values.ElementAt(0), content1);
            Assert.AreEqual(result.Values.ElementAt(1), content2);
        }

        [TestMethod]
        public async Task GetCmsContentItems_Handles_DuplicateCmsKeys()
        {
            var cmsKey1 = new CmsKey()
            {
                CmsId = "page.test-content1",
                AreaName = "test-area"
            };
            var cmsKey2 = new CmsKey()
            {
                CmsId = "page.test-content1",
                AreaName = "test-area"
            };

            var cultureCode = "en-US";
            var content1 = Mock.Of<Page>();

            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey1, cultureCode)).ReturnsAsync(content1);
            var result = await cmsService.GetCmsContentItemsAsync(new CmsKey[] { cmsKey1, cmsKey2 }, cultureCode);

            Assert.AreEqual(result.Keys.Count, 1);
            Assert.AreEqual(result.Keys.ElementAt(0), cmsKey1.CompositeKey);

            Assert.AreEqual(result.Values.ElementAt(0), content1);

            cmsLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CmsKey>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public async Task GetCmsContentItems_Handles_CaseSensitiveCmsKeys()
        {
            var cmsKey1 = new CmsKey()
            {
                CmsId = "page.test-content1",
                AreaName = "test-area"
            };
            var cmsKey2 = new CmsKey()
            {
                CmsId = "page.TEST-content1",
                AreaName = "test-area"
            };
            var cmsKey3 = new CmsKey()
            {
                CmsId = "page.test-content1",
                AreaName = "TEST-AREA"
            };
            var cmsKey4 = new CmsKey()
            {
                CmsId = "page.test-content2",
                AreaName = "TEST-AREA"
            };

            var cultureCode = "en-US";
            var content1 = Mock.Of<Page>();
            var content4 = Mock.Of<Page>();

            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey1, cultureCode)).ReturnsAsync(content1);
            cmsLoaderMock.Setup(l => l.LoadAsync(cmsKey4, cultureCode)).ReturnsAsync(content4);
            var result = await cmsService.GetCmsContentItemsAsync(new CmsKey[] { cmsKey1, cmsKey2, cmsKey3, cmsKey4 }, cultureCode);

            Assert.AreEqual(result.Keys.Count, 2);
            Assert.AreEqual(result.Keys.ElementAt(0), cmsKey1.CompositeKey);
            Assert.AreEqual(result.Keys.ElementAt(1), cmsKey4.CompositeKey);

            Assert.AreEqual(result.Values.ElementAt(0), content1);
            Assert.AreEqual(result.Values.ElementAt(1), content4);

            cmsLoaderMock.Verify(l => l.LoadAsync(It.IsAny<CmsKey>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [TestCleanup]
        public void Cleanup()
        {
            cmsLoaderMock.VerifyAll();
        }

    }
}
