namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class MigratingAgentValidationsForDeleteAgentWriterTest
    {
        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Create and no MigratingConnectionDetails, then succeed."), ValidData]
        public void When_WriteActionCreateAndNoMigratingConnectionDetails_Then_Succeed(
         DeleteAgent deleteAgent,
         DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = null;

            writer.ValidateMigratingConnectionDetails(WriteAction.Create, deleteAgent);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Create and MigratingConnectionDetails, then fail."), ValidData]
        public void When_WriteActionCreateAndMigratingConnectionDetails_Then_Fail(
         DeleteAgent deleteAgent,
         DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.PCFV2Batch,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Create, deleteAgent));

            Assert.Equal("migratingConnectionDetails", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, PPE MigratingConnectionDetails and PPE ConnectionDetails, then fail."), ValidData]
        public void When_UpdateAsyncWithTwoPpeConnectionDetails_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.PCFV2Batch,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("incomingEntity.ConnectionDetails", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are PCFV2Batch, then fail."), ValidData]
        public void When_UpdateAsyncWithBothPCFV2Batch_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.PCFV2Batch,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.PCFV2Batch,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("MigratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are CommandFeedV1, then fail."), ValidData]
        public void When_UpdateAsyncWithBothCommandFeedV1_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("migratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are CommandFeedV2, then fail."), ValidData]
        public void When_UpdateAsyncWithBothCommandFeedV2_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("MigratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are CosmosDeleteSignalV2, then fail."), ValidData]
        public void When_UpdateAsyncWithBothCosmosDeleteSignalV2_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("migratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are V1, then fail."), ValidData]
        public void When_UpdateAsyncWithBothV1_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2}
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("migratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, MigratingConnectionDetails and ConnectionDetails Protocols are V2, then fail."), ValidData]
        public void When_UpdateAsyncWithBothV2_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.PCFV2Batch,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            var exn = Assert.Throws<InvalidPropertyException>(() => writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent));

            Assert.Equal("MigratingConnectionDetail.protocol", exn.ParamName);
        }

        [Theory(DisplayName = "When ValidateMigratingConnectionDetails is called with WriteAction Update, then succeed."), ValidData]
        public void When_UpdateAsync_Then_Success(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV2,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.ProdReady,
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            writer.ValidateMigratingConnectionDetails(WriteAction.Update, deleteAgent);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {

            }
        }
    }
}