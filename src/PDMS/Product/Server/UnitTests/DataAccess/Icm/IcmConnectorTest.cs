namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Icm.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Icm = Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;

    public class IcmConnectorTest
    {
        [Theory(DisplayName = "When_UserIsNotInTheCorrectRole_Then_BlockAccess"), ValidData(treatAsIncidentManager: false)]
        public async Task When_UserIsNotInTheCorrectRole_Then_BlockAccess(
            Incident incident,
            IcmConnector connector)
        {
            await Assert.ThrowsAsync<MissingWritePermissionException>(() => connector.CreateIncidentAsync(incident)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When the disable title substitions property is set, then use the original string as is."), ValidData]
        public async Task When_DisableTitleFormat_Then_UseOriginalValue(
            Incident incident,
            IcmConnector connector)
        {
            incident.InputParameters.DisableTitleSubstitutions = true;
            incident.Title = "Test {0}";

            await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Assert.Equal("Test {0}", incident.Title);
        }

        [Theory(DisplayName = "When the disable body substitions property is set, then use the original string as is."), ValidData]
        public async Task When_DisableBodyFormat_Then_UseOriginalValue(
            Incident incident,
            IcmConnector connector)
        {
            incident.InputParameters.DisableBodySubstitutions = true;
            incident.Body = "Test {0}";

            await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Assert.Equal("Test {0}", incident.Body);
        }

        [Theory(DisplayName = "When the input parameters are not set, then use default values."), ValidData]
        public async Task When_NullInputParameters_Then_PerformDefaultBehavior(
            Incident incident,
            IcmConnector connector)
        {
            incident.InputParameters = null;
            incident.Title = "Title {0}";
            incident.Body = "Body {0}";

            await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Assert.NotEqual("Title {0}", incident.Title);
            Assert.NotEqual("Body {0}", incident.Body);
        }

        [Theory, ValidData]
        public async Task When_NoSourceIncidentSet_Then_IncludeSeverityInCorrelationId(
            Incident incident,
            [Frozen] Mock<Icm.IConnectorIncidentManager> icmClient,
            IcmConnector connector)
        {
            incident.AlertSourceId = null; // explicitly setting this, for clarity
            incident.Routing.AgentId = Guid.NewGuid();
            incident.Severity = 3;
            incident.Routing.EventName = "DeleteIncomplete";

            var expectedCorrelationId =
                $"{incident.Routing.AgentId.Value.ToString("N")}:{incident.Routing.EventName}:sev{incident.Severity}";

            await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            icmClient.Verify(x => x.AddOrUpdateIncident2(It.IsAny<Guid>(),
                It.Is<Icm.AlertSourceIncident>(a => a.CorrelationId == expectedCorrelationId),
                It.IsAny<Icm.RoutingOptions>()));
        }

        [Theory, ValidData]
        public async Task When_SourceIncidentSet_Then_DoNotIncludeSeverityInCorrelationId(
            Incident incident,
            [Frozen] Mock<Icm.IConnectorIncidentManager> icmClient,
            IcmConnector connector)
        {
            incident.AlertSourceId = "1234"; // some previous incident to explicitly update.
            incident.Routing.AgentId = Guid.NewGuid();
            incident.Severity = 3;
            incident.Routing.EventName = "DeleteIncomplete";

            var expectedCorrelationId =
                $"{incident.Routing.AgentId.Value.ToString("N")}:{incident.Routing.EventName}";

            await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            icmClient.Verify(x => x.AddOrUpdateIncident2(It.IsAny<Guid>(),
                It.Is<Icm.AlertSourceIncident>(a => a.CorrelationId == expectedCorrelationId),
                It.IsAny<Icm.RoutingOptions>()));
        }

        [Theory(DisplayName = "When alert source id set, then user that value."), ValidData]
        public async Task When_AlertSourceIdSet_Then_UseThatValue(
            Incident incident,
            [Frozen] Mock<Icm.IConnectorIncidentManager> icmClient,
            IcmConnector connector)
        {
            incident.AlertSourceId = "test";

            var result = await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Action<Icm.AlertSourceIncident> verify = v => Assert.Equal("test", v.Source.IncidentId);

            icmClient.Verify(x => x.AddOrUpdateIncident2(It.IsAny<Guid>(), Is.Value(verify), It.IsAny<Icm.RoutingOptions>()));

            Assert.Equal("test", result.AlertSourceId);
        }

        [Theory(DisplayName = "When alert source id is not set, then the service provides a value."), ValidData]
        public async Task When_AlertSourceIdNotSet_Then_UseServiceValue(
            Incident incident,
            [Frozen] Mock<Icm.IConnectorIncidentManager> icmClient,
            IcmConnector connector)
        {
            incident.AlertSourceId = null;

            var result = await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Action<Icm.AlertSourceIncident> verify = v => Guid.Parse(v.Source.IncidentId); // Throws if not a Guid.

            icmClient.Verify(x => x.AddOrUpdateIncident2(It.IsAny<Guid>(), Is.Value(verify), It.IsAny<Icm.RoutingOptions>()));

            Guid.Parse(result.AlertSourceId); // Throws if not a Guid.
        }

        [Theory(DisplayName = "Verity status and substatus responses."), ValidData]
        public async Task VerifyStatusAndSubstatusResponses(
            Incident incident,
            [Frozen] Icm.IncidentAddUpdateResult icmResponse,
            IcmConnector connector)
        {
            var result = await connector.CreateIncidentAsync(incident).ConfigureAwait(false);

            Assert.Equal(icmResponse.Status, (Icm.IncidentAddUpdateStatus)result.ResponseMetadata.Status);
            Assert.Equal(icmResponse.SubStatus, (Icm.IncidentAddUpdateSubStatus)result.ResponseMetadata.Substatus);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create, bool treatAsIncidentManager = true) : base(true)
            {
                this.Fixture.EnableIdentity();

                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();

                this.Fixture.Customize<AssetGroup>(obj =>
                    obj.With(x => x.Qualifier, this.Fixture.Create<AssetQualifier>()));

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups, treatAsIncidentManager: treatAsIncidentManager);
            }
        }
    }
}