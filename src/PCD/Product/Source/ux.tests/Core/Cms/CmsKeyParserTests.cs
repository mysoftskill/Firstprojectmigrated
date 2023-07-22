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
    public class CmsKeyParserTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parse_ThrowsException_When_CmsIdIsNull()
        {
            var cmsKeyParser = new CmsKeyParser();

            var parsedKey = cmsKeyParser.Parse(new CmsKey()
            {
                AreaName = "test-area",
                CmsId = null
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Parse_ThrowsException_When_CmsIdMissingType()
        {
            var cmsKeyParser = new CmsKeyParser();

            var parsedKey = cmsKeyParser.Parse(new CmsKey()
            {
                AreaName = "test-area",
                CmsId = "test-content"
            });
        }


        [TestMethod]
        public void Parses_CmsKey_ReturnsCorrectLocationAndType()
        {
            var cmsKeyParser = new CmsKeyParser();
            var parsedKey = cmsKeyParser.Parse(new CmsKey()
            {
                AreaName = "test",
                CmsId = "page.agent-health"
            });

            Assert.AreEqual(parsedKey.CmsLocation, "/test/agent-health");
            Assert.AreEqual(parsedKey.CmsTypeName, "page");
        }

        [TestMethod]
        public void Parses_CmsKey_ReturnsCorrectLocationAndPrefixedType()
        {
            var cmsKeyParser = new CmsKeyParser();
            var parsedKey = cmsKeyParser.Parse(new CmsKey()
            {
                AreaName = "test",
                CmsId = "page.special.agent-health"
            });

            Assert.AreEqual(parsedKey.CmsLocation, "/test/agent-health");
            Assert.AreEqual(parsedKey.CmsTypeName, "page.special");
        }

    }
}
