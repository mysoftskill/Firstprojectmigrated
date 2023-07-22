namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.ClientSdks
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class DataAgentParsingTests
    {
        [TestMethod]
        public void RoundTripAgentWithoutMigrationDetails()
        {
            Guid agentId = Guid.NewGuid();
            Guid owerId = Guid.NewGuid();

            var preprodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid(),
                ReleaseState = ReleaseState.PreProd,
            };
            var prodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
            };

            var batchWrapper = new ODataWrapper<DeleteAgent>() { Values = new List<DeleteAgent>() };
            batchWrapper.Values.Add(new DeleteAgent
            {
                Id = agentId.ToString(),
                Name = "My Agent is just OK.",
                Description = "Because it is mostly adequate",
                SharingEnabled = false,
                IsThirdPartyAgent = false,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.Public },
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { ReleaseState.PreProd, preprodConnection }, { ReleaseState.Prod, prodConnection } },
                Capabilities = new List<CapabilityId> { Policies.Current.Capabilities.Ids.Delete },
                OwnerId = owerId.ToString(),
                Icm = new Icm
                {
                    ConnectorId = Guid.NewGuid(),
                    Source = IcmSource.Manual,
                    TenantId = 123,
                },
            });

            var agentJson = JsonConvert.SerializeObject(batchWrapper);

            var restoredAgent = JsonConvert.DeserializeObject<ODataWrapper<DeleteAgent>>(agentJson);

            Assert.AreEqual(agentId.ToString(), restoredAgent.Values[0].Id);
        }

        [TestMethod]
        public void RoundTripAgentWithNullMigrationDetails()
        {
            Guid agentId = Guid.NewGuid();
            Guid owerId = Guid.NewGuid();

            var preprodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid(),
                ReleaseState = ReleaseState.PreProd,
            };
            var prodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
            };

            var batchWrapper = new ODataWrapper<DeleteAgent>() { Values = new List<DeleteAgent>() };
            batchWrapper.Values.Add(new DeleteAgent
            {
                Id = agentId.ToString(),
                Name = "My Agent is just OK.",
                Description = "Because it is mostly adequate",
                SharingEnabled = false,
                IsThirdPartyAgent = false,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.Public },
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { ReleaseState.PreProd, preprodConnection }, { ReleaseState.Prod, prodConnection } },
                MigratingConnectionDetails = null,
                Capabilities = new List<CapabilityId> { Policies.Current.Capabilities.Ids.Delete },
                OwnerId = owerId.ToString(),
                Icm = new Icm
                {
                    ConnectorId = Guid.NewGuid(),
                    Source = IcmSource.Manual,
                    TenantId = 123,
                },
            });

            var agentJson = JsonConvert.SerializeObject(batchWrapper);

            var restoredAgent = JsonConvert.DeserializeObject<ODataWrapper<DeleteAgent>>(agentJson);

            Assert.AreEqual(agentId.ToString(), restoredAgent.Values[0].Id);
        }

        [TestMethod]
        public void RoundTripAgentWithEmptyMigrationDetails()
        {
            Guid agentId = Guid.NewGuid();
            Guid owerId = Guid.NewGuid();

            var preprodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid(),
                ReleaseState = ReleaseState.PreProd,
            };
            var prodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
            };

            var batchWrapper = new ODataWrapper<DeleteAgent>() { Values = new List<DeleteAgent>() };
            batchWrapper.Values.Add(new DeleteAgent
            {
                Id = agentId.ToString(),
                Name = "My Agent is just OK.",
                Description = "Because it is mostly adequate",
                SharingEnabled = false,
                IsThirdPartyAgent = false,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.Public },
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { ReleaseState.PreProd, preprodConnection }, { ReleaseState.Prod, prodConnection } },
                MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>(),
                Capabilities = new List<CapabilityId> { Policies.Current.Capabilities.Ids.Delete },
                OwnerId = owerId.ToString(),
                Icm = new Icm
                {
                    ConnectorId = Guid.NewGuid(),
                    Source = IcmSource.Manual,
                    TenantId = 123,
                },
            });

            var agentJson = JsonConvert.SerializeObject(batchWrapper);

            var restoredAgent = JsonConvert.DeserializeObject<ODataWrapper<DeleteAgent>>(agentJson);

            Assert.AreEqual(agentId.ToString(), restoredAgent.Values[0].Id);
        }

        [TestMethod]
        public void RoundTripAgentWithMigrationDetails()
        {
            Guid agentId = Guid.NewGuid();
            Guid owerId = Guid.NewGuid();

            var preprodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid(),
                ReleaseState = ReleaseState.PreProd,
            };
            var prodConnection = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AadAppId = Guid.NewGuid(),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
            };

            var batchWrapper = new ODataWrapper<DeleteAgent>() { Values = new List<DeleteAgent>() };
            batchWrapper.Values.Add(new DeleteAgent
            {
                Id = agentId.ToString(),
                Name = "My Agent is just OK.",
                Description = "Because it is mostly adequate",
                SharingEnabled = false,
                IsThirdPartyAgent = false,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.Global,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.Public },
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { ReleaseState.PreProd, preprodConnection }, { ReleaseState.Prod, prodConnection } },
                MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                { { ReleaseState.PreProd, new ConnectionDetail
                        {
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            AadAppId = Guid.NewGuid(),
                            ReleaseState = ReleaseState.PreProd,
                        }
                    }
                },
                Capabilities = new List<CapabilityId> { Policies.Current.Capabilities.Ids.Delete },
                OwnerId = owerId.ToString(),
                Icm = new Icm
                {
                    ConnectorId = Guid.NewGuid(),
                    Source = IcmSource.Manual,
                    TenantId = 123,
                },
            });

            var agentJson = JsonConvert.SerializeObject(batchWrapper);

            var restoredAgent = JsonConvert.DeserializeObject<ODataWrapper<DeleteAgent>>(agentJson);

            Assert.AreEqual(agentId.ToString(), restoredAgent.Values[0].Id);
            Assert.AreEqual(1, restoredAgent.Values[0].MigratingConnectionDetails.Count);
            Assert.AreEqual(ReleaseState.PreProd, restoredAgent.Values[0].MigratingConnectionDetails[0].ReleaseState);
        }
    }

    /// <summary>
        /// Wrapper for OData batch results from PDMS.
        /// </summary>
        /// <typeparam name="T">Contained data type in the batch.</typeparam>
    public class ODataWrapper<T>
    {
        /// <summary>
        /// Gets or sets a list of values of the specified type in the batch.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public List<T> Values { get; set; }
    }
}
