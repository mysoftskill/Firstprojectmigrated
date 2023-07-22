// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class KustoQueryDefTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private void RunParamTest(
            string clusterUrl,
            string database,
            TemplateRef template,
            bool expected,
            string expectedCtxText)
        {
            KustoQueryDef def = new KustoQueryDef
            {
                Query = template,
                ClusterUrl = clusterUrl,
                Database = database
            };

            // test
            bool result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.AreEqual(expected, result);
            if (expectedCtxText != null)
            {
                this.mockCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains(expectedCtxText))), Times.Once);
            }
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfClusterUrlInvalid(string value)
        {
            TemplateRef tref = new TemplateRef { Inline = "text" };
            this.RunParamTest(value, "database", tref, false, "non-empty cluster URL");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfDatabaseInvalid(string value)
        {
            TemplateRef tref = new TemplateRef { Inline = "text" };
            this.RunParamTest("uri", value, tref, false, "non-empty database");
        }

        [TestMethod]
        public void ValidateReturnsFalseIfQueryNull()
        {
            this.RunParamTest("uri", "database", null, false, "query template must be specified");
        }

        [TestMethod]
        public void ValidateReturnsFalseIfQueryInvalid()
        {
            TemplateRef tref = new TemplateRef();
            this.RunParamTest("uri", "database", tref, false, null);
        }

        [TestMethod]
        public void ValidateReturnsTrueIfAllValid()
        {
            TemplateRef tref = new TemplateRef { Inline = "text" };
            this.RunParamTest("uri", "database", tref, true, null);
        }
    }
}
