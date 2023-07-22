namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
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
    public class DataAgentMigrationTests : TestBase
    {
        [TestMethod]
        public async Task MigrationWorks()
        {
            string agentPayload = $@"{{
                '@odata.type': '#v2.DeleteAgent',
                'name': '{Guid.NewGuid()}',
                'description': 'FunctionalTests Created Agent',
                'ownerId': 'd809230a-31a6-4644-9603-42945370a001',
                'deploymentLocation': 'Public',
                'dataResidencyBoundary': 'EU',
                'supportedClouds': ['All'],
                'connectionDetails': [
                {{
                  'protocol': 'CommandFeedV1',
                  'authenticationType': 'AadAppBasedAuth',
                  'msaSiteId': null,
                  'aadAppId': '2b55cd5e-3bb7-4eea-925e-f4d51b9cec16',
                  'aadAppIds': [],
                  'releaseState': 'PreProd',
                  'agentReadiness': 'ProdReady'
                }},
                {{
                  'protocol': 'CommandFeedV1',
                  'authenticationType': 'AadAppBasedAuth',
                  'msaSiteId': null,
                  'aadAppId': '2b55cd5e-3bb7-4eea-925e-f4d51b9cec25',
                  'aadAppIds': [],
                  'releaseState': 'Prod',
                  'agentReadiness': 'TestInProd'
                }}]
            }}";
            
            var agent = await CreateAgentAsync(agentPayload).ConfigureAwait(false);

            // PPE Migration
            agent.ConnectionDetails = new List<ConnectionDetail>
            {
                {
                    agent.ConnectionDetails.Single(c => c.ReleaseState == ReleaseState.Prod)
                }
            };
            agent.MigratingConnectionDetails = new List<ConnectionDetail>
            {

                {
                    new ConnectionDetail
                    {
                        AadAppIds = new List<Guid> { Guid.NewGuid() },
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady
                    }
                }
            };

            agent = await UpdateAgentAsync(agent).ConfigureAwait(false);

            var migratingConnectionDetails = (List<ConnectionDetail>)agent.MigratingConnectionDetails;

            Assert.AreEqual(migratingConnectionDetails.Count, 1);
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV2));

            var connectionDetails = (List<ConnectionDetail>)agent.ConnectionDetails;
            Assert.AreEqual(connectionDetails.Count, 1);
            Assert.IsFalse(connectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));

            Assert.IsTrue(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            // Prod Migration
            agent.MigratingConnectionDetails = connectionDetails;

            migratingConnectionDetails.Add(new ConnectionDetail()
            {
                AadAppIds = new List<Guid> { Guid.NewGuid() },
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd
            });
            agent.ConnectionDetails = migratingConnectionDetails;

            agent = await UpdateAgentAsync(agent).ConfigureAwait(false);

            migratingConnectionDetails = (List<ConnectionDetail>)agent.MigratingConnectionDetails;

            Assert.AreEqual(migratingConnectionDetails.Count, 1);
            Assert.IsFalse(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            connectionDetails = (List<ConnectionDetail>)agent.ConnectionDetails;
            Assert.AreEqual(connectionDetails.Count, 2);
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));

            Assert.IsTrue(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV2));
            Assert.IsFalse(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            // rollback
            agent.ConnectionDetails = migratingConnectionDetails;
            agent.MigratingConnectionDetails = connectionDetails;

            agent = await UpdateAgentAsync(agent).ConfigureAwait(false);

            migratingConnectionDetails = (List<ConnectionDetail>)agent.MigratingConnectionDetails;

            Assert.AreEqual(migratingConnectionDetails.Count, 2);
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV2));
            Assert.IsFalse(migratingConnectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            connectionDetails = (List<ConnectionDetail>)agent.ConnectionDetails;
            
            Assert.AreEqual(connectionDetails.Count, 1);
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));
            Assert.IsTrue(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            // rollforward
            agent.ConnectionDetails = migratingConnectionDetails;
            agent.MigratingConnectionDetails = connectionDetails;

            agent = await UpdateAgentAsync(agent).ConfigureAwait(false);

            migratingConnectionDetails = (List<ConnectionDetail>)agent.MigratingConnectionDetails;

            Assert.AreEqual(migratingConnectionDetails.Count, 1);
            Assert.IsFalse(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));
            Assert.IsTrue(migratingConnectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            connectionDetails = (List<ConnectionDetail>)agent.ConnectionDetails;
            Assert.AreEqual(connectionDetails.Count, 2);
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.PreProd));
            Assert.IsTrue(connectionDetails.Any(c => c.ReleaseState == ReleaseState.Prod));

            Assert.IsTrue(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV2));
            Assert.IsFalse(connectionDetails.Any(c => c.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1));

            await CleanupDataAgent(agent).ConfigureAwait(false);
        }

        private static async Task<MigratingDeleteAgent> CreateAgentAsync(string agentjson)
        {
            var content =
                await GetApiCallResponseAsStringAsync(
                    "/api/v2/dataAgents", 
                    HttpStatusCode.Created, 
                    HttpMethod.Post,
                    agentjson
                    ).ConfigureAwait(false);
            Assert.IsNotNull(content);
            return JsonConvert.DeserializeObject<MigratingDeleteAgent>(content);
        }

        private static async Task<MigratingDeleteAgent> UpdateAgentAsync(MigratingDeleteAgent agent)
        {
            var agentjson = JsonConvert.SerializeObject(agent);

            var content =
                await GetApiCallResponseAsStringAsync(
                    $"/api/v2/dataAgents('{agent.Id}')",
                    HttpStatusCode.OK,
                    HttpMethod.Put,
                    agentjson
                    ).ConfigureAwait(false);
            Assert.IsNotNull(content);

            return JsonConvert.DeserializeObject<MigratingDeleteAgent>(content);
        }

        private static async Task CleanupDataAgent(MigratingDeleteAgent agent)
        {
            try
            {
                await GetApiCallResponseAsStringAsync(
                    $"/api/v2/dataAgents('{agent.Id}')",
                    HttpStatusCode.OK,
                    HttpMethod.Delete,
                    etag: agent.ETag
                    ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // don't care here
            }
        }

        private class MigratingDeleteAgent 
        {
            [JsonProperty(PropertyName = "@odata.type", Order = -1)]
            public string ODataType 
            { 
                get
                {
                    return "#v2.DeleteAgent";
                }
            }

            [Key]
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "eTag")]
            public string ETag { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }

            [JsonProperty(PropertyName = "connectionDetails")]
            public IEnumerable<ConnectionDetail> ConnectionDetails { get; set; }

            [JsonProperty(PropertyName = "migratingConnectionDetails")]
            public IEnumerable<ConnectionDetail> MigratingConnectionDetails { get; set; }

            [JsonProperty(PropertyName = "capabilities")]
            public IEnumerable<string> Capabilities { get; set; }

            [JsonProperty(PropertyName = "ownerId")]
            public string OwnerId { get; set; }

            [JsonProperty(PropertyName = "deploymentLocation")]
            public string DeploymentLocation { get; set; }

            [JsonProperty(PropertyName = "dataResidencyBoundary")]
            public string DataResidencyBoundary { get; set; }

            [JsonProperty(PropertyName = "supportedClouds")]
            public IEnumerable<string> SupportedClouds { get; set; }
        }
    }
}
