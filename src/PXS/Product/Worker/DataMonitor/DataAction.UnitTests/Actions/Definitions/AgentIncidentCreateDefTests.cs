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
    public class AgentIncidentCreateDefTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private void RunParamTest(
            string eventName,
            TemplateRef keywords,
            TemplateRef title,
            TemplateRef body,
            bool expected,
            string expectedCtxText)
        {
            AgentIncidentCreateDef def = new AgentIncidentCreateDef
            {
                Keywords = keywords,
                Title = title,
                Body = body,

                EventName = eventName,
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
        public void ValidateReturnsFalseIfEventNameInvalid(string value)
        {
            TemplateRef tref = new TemplateRef { Inline = "text" };
            this.RunParamTest(value, tref, tref, tref, false, "non-empty EventName");
        }

        [TestMethod]
        [DataRow(0, "Keywords")]
        [DataRow(1, "Title")]
        [DataRow(2, "Body")]
        public void ValidateReturnsFalseIfTemplateNull(
            int index,
            string propName)
        {
            TemplateRef valid = new TemplateRef { Inline = "text" };
            TemplateRef[] set = { valid, valid, valid };
            string msg = propName + " template must be specified";

            set[index] = null;

            this.RunParamTest("event", set[0], set[1], set[2], false, msg);
        }

        [TestMethod]
        [DataRow(0, "Keywords")]
        [DataRow(1, "Title")]
        [DataRow(2, "Body")]
        public void ValidateReturnsFalseIfTemplateInvalid(
            int index,
            string propName)
        {
            TemplateRef invalid = new TemplateRef();
            TemplateRef valid = new TemplateRef { Inline = "text" };
            TemplateRef[] set = { valid, valid, valid };

            set[index] = invalid;

            this.RunParamTest("event", set[0], set[1], set[2], false, null);
        }

        [TestMethod]
        public void ValidateReturnsTrueIfAllValid()
        {
            TemplateRef tref = new TemplateRef { Inline = "text" };
            this.RunParamTest("event", tref, tref, tref, true, null);
        }
    }
}
