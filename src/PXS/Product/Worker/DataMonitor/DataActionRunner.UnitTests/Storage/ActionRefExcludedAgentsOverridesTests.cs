// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.UnitTests.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ActionRefExcludedAgentsOverridesTests
    {
        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>();
        private readonly ILogger mockLog = new MockLogger();

        private ActionRefExcludedAgentsOverrides testObj;
        private ExcludedAgentsFromAppConfig[] agents;
        private Dictionary<string, ICollection<ExcludedAgent>> agentMap;
        private string contents;

        [TestInitialize]
        public void Init()
        {
            this.SetupExcludedAgentsFromAppConfigurationTestData();

            this.contents = this.ReadOriginalActionRefJson();

            // Setup mock
            this.testObj = new ActionRefExcludedAgentsOverrides(this.mockAppConfiguration.Object, this.mockLog);
            this.mockAppConfiguration.Setup(o => o.GetConfigValues<ExcludedAgentsFromAppConfig>(It.IsAny<string>())).Returns(this.agents);
        }

        [TestMethod]
        public void MergeExcludedAgentsOverridesSucceeds()
        {
            // Test
            var result = this.testObj.MergeExcludedAgentsOverrides(this.contents);

            // Verify
            JArray jsonContents = JArray.Parse(result);

            foreach(var token in jsonContents)
            {
                string id = (string)token["Id"];
                var excludedAgents = token.SelectTokens("$..ExcludedAgentsJson").Children().ToList();

                // Original excluded agent lists should be replaced by the lists from App Configuration.
                Assert.AreEqual(this.agentMap[id].Count(), excludedAgents.Count());

                foreach (var agent in excludedAgents)
                {
                    string actual = (string)agent["Expires"];
                    string expected = this.agentMap[id]
                        .Where(a => string.Compare(a.AgentId, (string)agent["AgentId"], StringComparison.OrdinalIgnoreCase) == 0)
                        .FirstOrDefault()?.Expires;

                    // Expiration dates should match.
                    Assert.AreEqual(expected, actual);
                }
            }

            this.mockAppConfiguration.Verify(o => o.GetConfigValues<ExcludedAgentsFromAppConfig>(It.IsAny<string>()), Times.Once);
        }

        private string ReadOriginalActionRefJson()
        {
            using (
                Stream stream =
                    new FileStream("Storage\\ActionRefsTest.json", FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
            using (TextReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private void SetupExcludedAgentsFromAppConfigurationTestData()
        {
            this.agents = new ExcludedAgentsFromAppConfig[2];
            this.agents[0] = new ExcludedAgentsFromAppConfig
            {
                Id = "ExportSev2",
                ExcludedAgentsJson = new List<ExcludedAgent> {
                    new ExcludedAgent { AgentId = "00000000-0000-0000-0000-000000000000", Expires = "2030-01-01" },
                    new ExcludedAgent { AgentId = "00000000-0000-0000-0000-000000000001", Expires = "2025-12-01" }
                }
            };

            this.agents[1] = new ExcludedAgentsFromAppConfig
            {
                Id = "ExportSev3",
                ExcludedAgentsJson = new List<ExcludedAgent>
                {
                    new ExcludedAgent { AgentId = "00000000-0000-0000-0000-000000000000", Expires = "2024-12-01" },
                    new ExcludedAgent { AgentId = "00000000-0000-0000-0000-000000000001", Expires = "2027-05-18" }
                }
            };

            this.agentMap = new Dictionary<string, ICollection<ExcludedAgent>>();
            this.agentMap[this.agents[0].Id] = this.agents[0].ExcludedAgentsJson;
            this.agentMap[this.agents[1].Id] = this.agents[1].ExcludedAgentsJson;
        }
    }
}
