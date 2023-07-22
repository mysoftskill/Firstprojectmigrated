using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Core.Cms.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PrivacyServices.UX.Tests.Core.Cms
{
    [TestClass]
    public class CmsKeyTests
    {
        [TestMethod]
        public void CmsKey_Gets_CompositeKey()
        {
            var cmsKey = new CmsKey()
            {
                CmsId = "page.test-content",
                AreaName = "test-area"
            };

            Assert.AreEqual("PAGE.TEST-CONTENT@TEST-AREA", cmsKey.CompositeKey);
        }

        [TestMethod]
        public void CmsKey_Equals_Works()
        {
            var cmsKey1 = new CmsKey
            {
                CmsId = "page.test-content",
                AreaName = "test-area"
            };

            var cmsKey2 = new CmsKey
            {
                CmsId = "PAGE.TEST-CONTENT",
                AreaName = "TEST-AREA"
            };

            var cmsKey3 = new CmsKey
            {
                CmsId = "component.test-content",
                AreaName = "test-area"
            };

            var cmsKey4 = new CmsKey
            {
                CmsId = "component.test-content",
                AreaName = "test-area1"
            };

            Assert.IsTrue(cmsKey1.Equals(cmsKey2));
            Assert.IsFalse(cmsKey1.Equals(cmsKey3));
            Assert.IsFalse(cmsKey1.Equals(cmsKey4));
            Assert.IsFalse(cmsKey2.Equals(cmsKey3));
            Assert.IsFalse(cmsKey2.Equals(cmsKey4));
            Assert.IsFalse(cmsKey3.Equals(cmsKey4));
        }
    }
}
