namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;

    using Xunit;

    public class DataAgentWriter_T_Test
    {
        [Theory(DisplayName = "Verify valid connection details combinations.")]
        [PDDeleteV1.Inline(nameof(Policy.Policies.Current.Protocols.Ids.CommandFeedV1))]
        [PCFV1.Inline(nameof(Policy.Policies.Current.Protocols.Ids.CommandFeedV1))]
        [CosmosV1.Inline(nameof(Policy.Policies.Current.Protocols.Ids.CosmosDeleteSignalV2))]
        public void VerifyValidationSucceeds(ConnectionDetail connectionDetails)
        {
            this.VerifySuccess(connectionDetails);
        }

        [Theory(DisplayName = "When aad app id is missing, then validation fails.")]
        [PCFV1.Inline(nameof(Policy.Policies.Current.Protocols.Ids.CommandFeedV1))]
        public void VerifyNullAadAppIdValidationFails(ConnectionDetail connectionDetail)
        {
            connectionDetail.AadAppId = null;
            this.VerifyMissingProperty(connectionDetail);
        }

        private void VerifySuccess(ConnectionDetail connectionDetail)
        {
            MockDataAgentWriter.ValidateConnectionDetail(connectionDetail);
        }

        private void VerifyMissingProperty(ConnectionDetail connectionDetail)
        {
            Assert.Throws<MissingPropertyException>(() => MockDataAgentWriter.ValidateConnectionDetail(connectionDetail));
        }

        private void VerifyInvalidProperty(ConnectionDetail connectionDetail)
        {
            Assert.Throws<InvalidPropertyException>(() => MockDataAgentWriter.ValidateConnectionDetail(connectionDetail));
        }

        private DataAgent CreateAgent(ConnectionDetail connectionDetails)
        {
            var deleteAgent = new DeleteAgent();
            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(connectionDetails);
            deleteAgent.Name = "test";
            deleteAgent.Description = "test";
            return deleteAgent;
        }

        #region AutoFixture Custom Attributes
        public class MockDataAgentWriter : DataAgentWriter<DataAgent, DataAgentFilterCriteria>
        {
            public MockDataAgentWriter(IPrivacyDataStorageWriter storageWriter, IDataAgentReader<DataAgent> entityReader, AuthenticatedPrincipal authenticatedPrincipal, IAuthorizationProvider authorizationProvider, IDateFactory dateFactory, IMapper mapper, IDataOwnerReader dataOwnerReader, IValidator validator, IIcmConnector icmConnector, IEventWriterFactory eventWriterFactory)
                : base(storageWriter, entityReader, authenticatedPrincipal, authorizationProvider, dateFactory, mapper, dataOwnerReader, validator, icmConnector, eventWriterFactory)
            {
            }

            protected override Policy.ProtocolId[] ValidProtocols
            {
                get
                {
                    return Policy.Policies.Current.Protocols.Set.Select(v => v.Id).ToArray();
                }
            }

            protected override ReleaseState[] ValidReleaseStates
            {
                get
                {
                    return Enum.GetValues(typeof(ReleaseState)).Cast<ReleaseState>().ToArray();
                }
            }

            public override Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, DataAgent incomingEntity)
            {
                return Task.FromResult(Enumerable.Empty<DataOwner>());
            }
        }

        public class PDDeleteV1 : AutoMoqDataAttribute
        {
            public PDDeleteV1(string protocol) : base(true)
            {
                var connectionDetails = new ConnectionDetail();
                connectionDetails.Protocol = Policy.Policies.Current.Protocols.CreateId(protocol);
                connectionDetails.AuthenticationType = AuthenticationType.MsaSiteBasedAuth;
                connectionDetails.MsaSiteId = this.Fixture.Create<long?>();
                connectionDetails.AgentReadiness = connectionDetails.ReleaseState == ReleaseState.PreProd ? AgentReadiness.ProdReady : this.Fixture.Create<AgentReadiness>();
                this.Fixture.Inject(connectionDetails);
            }

            public class Inline : InlineAutoMoqDataAttribute
            {
                public Inline(string protocol, params object[] values) : base(new PDDeleteV1(protocol), values)
                {
                }
            }
        }

        public class PCFV1 : AutoMoqDataAttribute
        {
            public PCFV1(string protocol) : base(true)
            {
                var connectionDetails = new ConnectionDetail();
                connectionDetails.Protocol = Policy.Policies.Current.Protocols.CreateId(protocol);
                connectionDetails.AuthenticationType = AuthenticationType.AadAppBasedAuth;
                connectionDetails.AadAppId = this.Fixture.Create<Guid?>();
                connectionDetails.AgentReadiness = connectionDetails.ReleaseState == ReleaseState.PreProd ? AgentReadiness.ProdReady : this.Fixture.Create<AgentReadiness>();
                this.Fixture.Inject(connectionDetails);
            }

            public class Inline : InlineAutoMoqDataAttribute
            {
                public Inline(string protocol, params object[] values) : base(new PCFV1(protocol), values)
                {
                }
            }
        }

        public class CosmosV1 : AutoMoqDataAttribute
        {
            public CosmosV1(string protocol) : base(true)
            {
                var connectionDetails = new ConnectionDetail();
                connectionDetails.Protocol = Policy.Policies.Current.Protocols.CreateId(protocol);
                connectionDetails.AgentReadiness = connectionDetails.ReleaseState == ReleaseState.PreProd ? AgentReadiness.ProdReady : this.Fixture.Create<AgentReadiness>();
                this.Fixture.Inject(connectionDetails);
            }

            public class Inline : InlineAutoMoqDataAttribute
            {
                public Inline(string protocol, params object[] values) : base(new CosmosV1(protocol), values)
                {
                }
            }
        }

        public class PCFV2Batch : AutoMoqDataAttribute
        {
            public PCFV2Batch(string protocol) : base(true)
            {
                var connectionDetails = new ConnectionDetail();
                connectionDetails.Protocol = Policy.Policies.Current.Protocols.CreateId(protocol);
                connectionDetails.AuthenticationType = AuthenticationType.AadAppBasedAuth;
                connectionDetails.AadAppId = this.Fixture.Create<Guid?>();
                connectionDetails.AgentReadiness = connectionDetails.ReleaseState == ReleaseState.PreProd ? AgentReadiness.ProdReady : this.Fixture.Create<AgentReadiness>();
                this.Fixture.Inject(connectionDetails);
            }

            public class Inline : InlineAutoMoqDataAttribute
            {
                public Inline(string protocol, params object[] values) : base(new PCFV2Batch(protocol), values)
                {
                }
            }
        }
        #endregion
    }
}