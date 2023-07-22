namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class DataAgentTests : TestBase
    {
        /*
         * 1. create agent: exception cases
         * 2. delete agent with assets, pending commands, override
         * 3. add assetgroup to agent, transfer agent, remove assetgroup to agent etc
         * 
         */
        [TestMethod]
        public async Task CanReadDataAgentsUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            var agentsResponse = await TestSetup.PdmsClientInstance.DataAgents.ReadAllByFiltersAsync<DeleteAgent>(
                    TestSetup.RequestContext,
                    DataAgentExpandOptions.None)
                .ConfigureAwait(false);

            Assert.IsTrue(agentsResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentsResponse.HttpStatusCode}");

            Assert.IsTrue(agentsResponse.Response.Any());

            if (agentsResponse.Response.Any(a => a.Id.Equals(agent.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await GetAnAgentAndAssert(agent.Id).ConfigureAwait(false);

                await CleanupDataAgent(agent).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Newly created DeleteAgent not retrieved");
            }
        }

        [TestMethod]
        public async Task CanCreateADataAgentUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            await GetAnAgentAndAssert(agent.Id).ConfigureAwait(false);

            await CleanupDataAgent(agent).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanDeleteADataAgentUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .DeleteAsync(agent.Id, agent.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.NoContent,
            $"StatusCode was {agentResponse.HttpStatusCode}");

            await GetApiCallResponseAsStringAsync(
                    $"api/v2/DataAgents/{agent.Id}",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanUpdateADataAgentUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            agent.ConnectionDetails.Add(ReleaseState.Prod, new ConnectionDetail()
            {
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.IsTrue(agentResponse.Response.ConnectionDetails.Count == 2);

            await CleanupDataAgent(agentResponse.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanChangeConnectionDetailsProtocolUsingClient()
        {
            // Create an agent with PreProd command feed connection
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            // Add a Prod connection
            agent.ConnectionDetails.Add(ReleaseState.Prod, new ConnectionDetail()
            {
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.IsTrue(agentResponse.Response.ConnectionDetails.Count == 2);

            // Change the protocol and null out any of the rest of the fields for both
            // the Prod and PreProd connection details
            agent = agentResponse.Response;
            var connectionDetails = agent.ConnectionDetails[ReleaseState.PreProd];
            connectionDetails.Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2;
            connectionDetails.AuthenticationType = null;
            connectionDetails.AadAppId = null;
            connectionDetails.AadAppIds = null;
            connectionDetails.MsaSiteId = null;
            connectionDetails = agent.ConnectionDetails[ReleaseState.Prod];
            connectionDetails.Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2;
            connectionDetails.AuthenticationType = null;
            connectionDetails.AadAppId = null;
            connectionDetails.AadAppIds = null;
            connectionDetails.MsaSiteId = null;

            // Update the agent
            var agentResponse2 = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            // Check that the update was successful
            Assert.IsTrue(agentResponse2.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse2.HttpStatusCode}");
            Assert.IsTrue(agentResponse2.Response.ConnectionDetails.Count == 2);

            // Check the new protocol
            Assert.IsTrue(agentResponse2.Response.ConnectionDetails[ReleaseState.PreProd].Protocol == Policies.Current.Protocols.Ids.CosmosDeleteSignalV2);
            Assert.IsTrue(agentResponse2.Response.ConnectionDetails[ReleaseState.Prod].Protocol == Policies.Current.Protocols.Ids.CosmosDeleteSignalV2);

            await CleanupDataAgent(agentResponse2.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanCreateADataAgentWithOnlyAppIdArrayUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            agent.ConnectionDetails.Remove(ReleaseState.PreProd);

            agent.ConnectionDetails.Add(ReleaseState.PreProd, new ConnectionDetail()
            {
                AadAppIds = Enumerable.Empty<Guid>().Append(Guid.NewGuid()).Append(Guid.NewGuid()),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.IsTrue(agentResponse.Response.ConnectionDetails.Count == 1);

            agentResponse.Response.ConnectionDetails.TryGetValue(ReleaseState.PreProd, out ConnectionDetail responseConnection);

            Assert.IsTrue(responseConnection.AadAppIds.Count() == 2);

            await CleanupDataAgent(agentResponse.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanUpdateADataAgentToDualAuthUsingClient()
        {
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            agent.ConnectionDetails.Remove(ReleaseState.PreProd);

            agent.ConnectionDetails.Add(ReleaseState.PreProd, new ConnectionDetail()
            {
                AadAppIds = Enumerable.Empty<Guid>().Append(Guid.NewGuid()).Append(Guid.NewGuid()),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.IsTrue(agentResponse.Response.ConnectionDetails.Count == 1);

            agentResponse.Response.ConnectionDetails.TryGetValue(ReleaseState.PreProd, out ConnectionDetail responseConnection);

            Assert.IsTrue(responseConnection.AadAppIds.Count() == 2);

            await CleanupDataAgent(agentResponse.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanRemoveDualAuthAgentAppIdUsingClient()
        {
            var agent = await CreateNewDualAuthDataAgentAsync().ConfigureAwait(false);

            agent.ConnectionDetails.Remove(ReleaseState.PreProd);

            agent.ConnectionDetails.Add(ReleaseState.PreProd, new ConnectionDetail()
            {
                AadAppIds = Enumerable.Empty<Guid>().Append(Guid.NewGuid()),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.IsTrue(agentResponse.Response.ConnectionDetails.Count == 1);

            agentResponse.Response.ConnectionDetails.TryGetValue(ReleaseState.PreProd, out ConnectionDetail responseConnection);

            Assert.IsTrue(responseConnection.AadAppIds.Count() == 1);

            await CleanupDataAgent(agentResponse.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanReadAllDataAgentsCallingApi()
        {
            // Make sure there is at least one agent to read
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    "api/v2/DataAgents/v2.DeleteAgent?$select=id,eTag,name,description,connectionDetails,migratingConnectionDetails,ownerId,capabilities,operationalReadinessLow,operationalReadinessHigh,icm,inProdDate,sharingEnabled,isThirdPartyAgent,deploymentLocation,supportedClouds,dataResidencyBoundary")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var dataAgents = JsonConvert.DeserializeObject<ODataResponse<List<DeleteAgent>>>(content);
            Assert.IsTrue(dataAgents.Value.Count > 0);

            await CleanupDataAgent(agent).ConfigureAwait(false);
        }

        private static async Task GetAnAgentAndAssert(string agentId)
        {
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .ReadAsync<DeleteAgent>(agentId, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {agentResponse.HttpStatusCode}");
            Assert.AreEqual(agentId, agentResponse.Response.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument))]
        public async Task WhenICallCreateDataAgentsWithNullDataAgentItFailsAsync()
        {
            DataAgent dataAgent = null;
            await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WhenICallCreateDataAgentsWithoutResidencyItFailsAsync()
        {
            DataAgent dataAgent = new DeleteAgent()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                HasSharingRequests = false,
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false),
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.All }
            };

            try
            {
                await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
            }
            catch ( BadArgumentError.NullArgument error )
            {
                Assert.AreEqual("Value must not be omitted or null.", error.Message);
                Assert.AreEqual("dataResidencyBoundary", error.Target);
            }
        }

        [TestMethod]
        public async Task WhenICallCreateDataAgentsWithoutResidencyDeploymentInMooncakeAsync()
        {
            DataAgent dataAgent = new DeleteAgent()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                HasSharingRequests = false,
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false),
                DeploymentLocation = Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake }
            };

            var agentResponse=await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsNotNull(agentResponse);
        }

        [TestMethod]
        public async Task WhenICallCreateDataAgentsWithoutResidencyDeploymentInFairfaxAsync()
        {
            DataAgent dataAgent = new DeleteAgent()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                HasSharingRequests = false,
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false),
                DeploymentLocation = Policies.Current.CloudInstances.Ids.US_Azure_Fairfax,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.US_Azure_Fairfax }
            };

            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsNotNull(agentResponse);
        }

        [TestMethod]
        public async Task WhenICallCreateDataAgentsWithResidencyDeploymentInMooncakeItFailsAsync()
        {
            DataAgent dataAgent = new DeleteAgent()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                HasSharingRequests = false,
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false),
                DeploymentLocation = Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake },
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global
            };
            try
            {
                await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                    .ConfigureAwait(false);
            }
            catch (BadArgumentError.InvalidArgument error)
            {
                Assert.AreEqual("In case of sovereign cloud agent, data residency should not be set", error.Message);
                Assert.AreEqual("dataResidencyBoundary", error.Target);
            }
        }

        [TestMethod]
        public async Task WhenICallCreateDataAgentsWithResidencyDeploymentInFairfaxItFailsAsync()
        {
            DataAgent dataAgent = new DeleteAgent()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                HasSharingRequests = false,
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false),
                DeploymentLocation = Policies.Current.CloudInstances.Ids.US_Azure_Fairfax,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.US_Azure_Fairfax },
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global
            };
            try
            {
                await TestSetup.PdmsClientInstance.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                    .ConfigureAwait(false);
            }
            catch (BadArgumentError.InvalidArgument error)
            {
                Assert.AreEqual("In case of sovereign cloud agent, data residency should not be set", error.Message);
                Assert.AreEqual("dataResidencyBoundary", error.Target);
            }
        }

        [TestMethod]
        public async Task WhenICallApiToReadAllDataAgentsUsingHeadMethodItFailsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("api/v2/DataAgents", HttpStatusCode.BadRequest, HttpMethod.Head)
                .ConfigureAwait(false);
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }
    }
}
