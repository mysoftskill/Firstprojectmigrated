// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Incidents
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AgentIncidenTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private void RunParamTest(
            string agentId,
            string assetGroupId,
            string ownerId,
            string eventName,
            int severity,
            string title,
            string body,
            bool expected,
            string expectedCtxText)
        {
            AgentIncident def = new AgentIncident
            {
                AgentId = agentId,
                OwnerId = ownerId,
                AssetGroupId = assetGroupId,

                Keywords = "keywords",
                Title = title,
                Body = body,

                EventName = eventName,
                Severity = severity,
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
        [DataRow(false, false, false, false, "at least one")]
        [DataRow(true, false, false, true, null)]
        [DataRow(true, false, true, true, null)]
        [DataRow(true, false, true, false, null)]
        [DataRow(true, true, false, true, null)]
        [DataRow(true, true, false, false, null)]
        [DataRow(true, true, true, true, null)]
        [DataRow(true, true, true, false, null)]
        public void ValidateReturnsCorrectValueForOwnershupFieldsValidation(
            bool expected,
            bool hasOwner,
            bool hasAgent,
            bool hasAsset,
            string expectedCtxText = null)
        {
            const string Id = "id";
            string assetGroupId = hasAsset ? Id : null;
            string agentId = hasAgent ? Id : null;
            string ownerId = hasOwner ? Id : null;

            this.RunParamTest(
                agentId, assetGroupId, ownerId, "event", 3, "title", "body", expected, expectedCtxText);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfEventNameInvalid(string value)
        {
            const string Id = "id";
            this.RunParamTest(Id, Id, Id, value, 3, "title", "body", false, "event name must be non-empty");
        }

        [TestMethod]
        [DataRow(AgentIncident.MinSev - 1)]
        [DataRow(AgentIncident.MaxSev + 1)]
        public void ValidateReturnsFalseIfSeverityInvalid(int value)
        {
            const string Id = "id";
            this.RunParamTest(Id, Id, Id, "event", value, "title", "body", false, "severity must be in the range");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfTitleInvalid(string value)
        {
            const string Id = "id";
            this.RunParamTest(Id, Id, Id, "event", 3, value, "body", false, "title must be non-empty");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfBodyInvalid(string value)
        {
            const string Id = "id";
            this.RunParamTest(Id, Id, Id, "event", 3, "title", value, false, "body must be non-empty");
        }

        [TestMethod]
        public void ValidateReturnsTrueIfAllValid()
        {
            const string Id = "id";
            this.RunParamTest(Id, Id, Id, "event", 3, "title", "body", true, null);
        }
    }
}
