namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;
    using Ploeh.AutoFixture;

    public static class AutofixtureCustomizations
    {
        public class TypeCorrectionsAttribute : AutoMoqDataAttribute
        {
            public TypeCorrectionsAttribute()
                : base(true)
            {
                this.Fixture.Customizations.Add(new UriSpecimenBuilder());
                this.Fixture.Customizations.Add(new IdSpecimenBuilder());
                this.Fixture.Customizations.Add(new QualifierSpecimenBuilder());

                this.Fixture.Customize<DataOwner>(entity =>
                    entity
                    .With(m => m.AlertContacts, this.Fixture.Create<IEnumerable<MailAddress>>().Select(m => m.Address))
                    .With(m => m.AnnouncementContacts, this.Fixture.Create<IEnumerable<MailAddress>>().Select(m => m.Address))
                    .With(m => m.WriteSecurityGroups, this.Fixture.Create<IEnumerable<Guid>>().Select(m => m.ToString()))
                    .With(m => m.SharingRequestContacts, this.Fixture.Create<IEnumerable<MailAddress>>().Select(m => m.Address))
                    .Without(m => m.DataAgents));

                this.Fixture.Customize<AssetGroup>(entity =>
                    entity
                    .With(m => m.OptionalFeatures, this.Fixture.Create<IEnumerable<Policy.OptionalFeatureId>>().Select(m => m.Value))
                    .Without(m => m.DeleteAgent)
                    .Without(m => m.ExportAgent)
                    .Without(m => m.AccountCloseAgent)
                    .Without(m => m.Inventory)); // Not sure why this causes issues.

                this.Fixture.Customize<VariantDefinition>(entity =>
                    entity
                    .With(m => m.DataTypes, this.Fixture.Create<IEnumerable<Policy.DataTypeId>>().Select(m => m.Value))
                    .With(m => m.Capabilities, this.Fixture.Create<IEnumerable<Policy.CapabilityId>>().Select(m => m.Value))
                    .With(m => m.SubjectTypes, this.Fixture.Create<IEnumerable<Policy.SubjectTypeId>>().Select(m => m.Value)));

                this.Fixture.Customize<AssetGroupVariant>(entity =>
                    entity
                    .With(m => m.TfsTrackingUris, this.Fixture.Create<IEnumerable<Uri>>().Select(m => m.ToString())));

                this.Fixture.Customize<ConnectionDetail>(entity =>
                    entity
                    .With(m => m.Protocol, this.Fixture.Create<Policy.ProtocolId>().Value)
                    .With(m => m.AuthenticationType, this.Fixture.Create<AuthenticationType>())
                    .With(m => m.ReleaseState, this.Fixture.Create<ReleaseState>())
                    .Without(m => m.AadAppIds));

                this.Fixture.Customize<DeleteAgent>(entity =>
                    entity
                    .With(m => m.Capabilities, this.Fixture.Create<IEnumerable<Policy.CapabilityId>>().Select(m => m.Value))
                    .With(m => m.DeploymentLocation, this.Fixture.Create<Policy.CloudInstanceId>().Value)
                    .With(m => m.DataResidencyBoundary, this.Fixture.Create<Policy.DataResidencyInstanceId>().Value)
                    .With(m => m.SupportedClouds, this.Fixture.Create<IEnumerable<Policy.CloudInstanceId>>().Select(m => m.Value))
                    .With(m => m.ConnectionDetails, this.Fixture.Create<IEnumerable<ConnectionDetail>>().Where(m => m.ReleaseState == ReleaseState.PreProd || m.ReleaseState == ReleaseState.Prod) )
                    .With(m => m.MigratingConnectionDetails, this.Fixture.Create<IEnumerable<ConnectionDetail>>().Where(m => m.ReleaseState == ReleaseState.PreProd))
                    .Without(m => m.AssetGroups));

                this.Fixture.Customize<DataAgent>(entity =>
                    entity
                    .FromFactory<int>(i =>
                    {
                        return this.Fixture.Create<DeleteAgent>();
                    })
                    .Without(m => m.Owner));

                this.Fixture.Customize<Models.V2.ConnectionDetail>(entity =>
                    entity
                    .With(m => m.Protocol, this.Fixture.Create<Policy.ProtocolId>())
                    .With(m => m.AuthenticationType, this.Fixture.Create<Models.V2.AuthenticationType>()+1) // +1 to correct for an off-by one difference in the models
                    .With(m => m.ReleaseState, this.Fixture.Create<Models.V2.ReleaseState>())
                    .Without(m => m.AadAppIds));

                this.Fixture.Customize<Models.V2.DataOwner>(entity =>
                    entity
                    .Without(m => m.DataAgents));

                this.Fixture.Customize<Models.V2.AssetGroup>(entity =>
                    entity
                    .With(m => m.OptionalFeatures, this.Fixture.Create<IEnumerable<Policy.OptionalFeatureId>>())
                    .Without(x => x.QualifierParts)
                    .Do(x => x.QualifierParts = this.Fixture.Create<AssetQualifier>().Properties)
                    .Without(m => m.DeleteAgent)); // Not sure why this causes issues.

                this.Fixture.Customize<Models.V2.Inventory>(entity =>
                    entity
                    .Without(m => m.Owner));

                this.Fixture.Customize<Models.V2.VariantDefinition>(entity =>
                    entity
                    .Without(m => m.Owner));

                this.Fixture.Customize<Models.V2.DeleteAgent>(entity =>
                    entity
                    .With(m => m.Capabilities, this.Fixture.Create<IEnumerable<Policy.CapabilityId>>())
                    .With(m => m.DeploymentLocation, this.Fixture.Create<Policy.CloudInstanceId>())
                    .With(m => m.DataResidencyBoundary, this.Fixture.Create<Policy.DataResidencyInstanceId>())
                    .With(m => m.SupportedClouds, this.Fixture.Create<IEnumerable<Policy.CloudInstanceId>>())
                    .With(m => m.ConnectionDetails, CreateConnectionDetails(this.Fixture.Create<Models.V2.ConnectionDetail>()))
                    .With(m => m.MigratingConnectionDetails, CreateConnectionDetails(this.Fixture.Create<Models.V2.ConnectionDetail>()))
                    .Without(m => m.AssetGroups));

                this.Fixture.Customize<Models.V2.DataAgent>(entity =>
                    entity.FromFactory<int>(i =>
                    {
                        return this.Fixture.Create<Models.V2.DeleteAgent>();
                    })
                    .Without(m => m.Owner));

                this.Fixture.Customize<Models.V2.HistoryItem>(entity =>
                    entity
                    .With(m => m.Entity, this.Fixture.Create<Models.V2.DataOwner>()));

                this.Fixture.Customize<SetAgentRelationshipParameters.Action>(x =>
                    x.With(m => m.CapabilityId, this.Fixture.Create<Policy.CapabilityId>().Value));
            }

            public static IDictionary<Models.V2.ReleaseState, Models.V2.ConnectionDetail> CreateConnectionDetails(Models.V2.ConnectionDetail connectionDetails)
            {
                return new Dictionary<Models.V2.ReleaseState, Models.V2.ConnectionDetail> { { connectionDetails.ReleaseState, connectionDetails } };
            }
        }

        public class InlineTypeCorrectionsAttribute : InlineAutoMoqDataAttribute
        {
            public InlineTypeCorrectionsAttribute(params object[] values)
            : base(new TypeCorrectionsAttribute(), values)
            {
            }
        }
    }
}