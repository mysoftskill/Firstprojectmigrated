using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Ploeh.AutoFixture;
using Microsoft.PrivacyServices.Identity;
using Microsoft.PrivacyServices.Identity.Metadata;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PrivacyPolicies = Microsoft.PrivacyServices.Policy;
using Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Utilities
{
    public static class Fixtures
    {
        public static IFixture Initialize(this IFixture fixture)
        {
            fixture.EnablePolicy().EnableIdentity();

            fixture.Customize<PdmsApiModelsV2.Entity>(entity =>
                entity
                .Without(x => x.Id)
                .Without(x => x.ETag)
                .Without(x => x.TrackingDetails));

            fixture.Customize<PdmsApiModelsV2.DataOwner>(entity =>
                entity
                .With(m => m.WriteSecurityGroups, new[] { fixture.Create<Guid>().ToString() })
                .With(m => m.AlertContacts, fixture.Create<IEnumerable<MailAddress>>().Select(m => m.Address))
                .With(m => m.AnnouncementContacts, fixture.Create<IEnumerable<MailAddress>>().Select(m => m.Address))
                .Without(x => x.DataAgents)
                .Without(x => x.AssetGroups)
                .Without(x => x.ServiceTree));

            fixture.Customize<PdmsApiModelsV2.AssetGroup>(entity =>
                entity
                .Without(m => m.DeleteAgentId)
                .Without(m => m.ExportAgentId)
                .Without(m => m.AccountCloseAgentId)
                .Without(m => m.InventoryId)
                .Without(m => m.Variants)
                .Without(m => m.DataAssets)
                .Without(m => m.DeleteAgent)
                .Without(m => m.ExportAgent)
                .Without(m => m.AccountCloseAgent)
                .Without(m => m.Inventory)
                .Without(m => m.Owner));

            fixture.Customize<PdmsApiModelsV2.Inventory>(entity =>
                entity
                .With(m => m.DataCategory, PdmsApiModelsV2.DataCategory.Controller)
                .With(m => m.RetentionPolicy, PdmsApiModelsV2.RetentionPolicy.EighteenMonths)
                .With(m => m.DisposalMethod, PdmsApiModelsV2.DisposalMethod.FullDelete)
                .With(m => m.DocumentationLink, fixture.Create<Uri>().ToString())
                .With(m => m.ThirdPartyRelation, PdmsApiModelsV2.ThirdPartyRelation.SentTo)
                .Without(m => m.Owner));

            fixture.Customize<PdmsApiModelsV2.VariantDefinition>(entity =>
                entity
                .Without(m => m.Owner));

            fixture.Customize<PdmsApiModelsV2.AssetGroupVariant>(entity =>
                entity
                .With(m => m.VariantState, PdmsApiModelsV2.VariantState.Approved)
                .With(m => m.TfsTrackingUris, fixture.Create<IEnumerable<Uri>>().Select(v => v.ToString())));

            fixture.Customize<PdmsApiModelsV2.ConnectionDetail>(entity =>
                entity
                .With(m => m.Protocol, PrivacyPolicies.Policies.Current.Protocols.Ids.CommandFeedV1)
                .With(m => m.AuthenticationType, PdmsApiModelsV2.AuthenticationType.MsaSiteBasedAuth)
                .Without(m => m.MsaSiteId)
                .Without(m => m.AadAppId));

            fixture.Customize<PdmsApiModelsV2.DeleteAgent>(entity =>
                entity
                .Without(m => m.AssetGroups)
                .Without(m => m.Capabilities)
                .Without(m => m.SupportedClouds)
                .Without(m => m.DeploymentLocation)
                .Do(agent => agent.ConnectionDetails = fixture.CreateMany<PdmsApiModelsV2.ConnectionDetail>(1).ToDictionary(v => v.ReleaseState)));

            fixture.Customize<PdmsApiModelsV2.DataAgent>(entity =>
                entity
                .Without(m => m.ConnectionDetails)
                .Without(m => m.Owner));

            return fixture;
        }

        /// <summary>
        /// Updates the fixture so that Microsoft.PrivacyServices.Policy data can be mocked.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture EnablePolicy(this IFixture fixture)
        {
            fixture.Inject(PrivacyPolicies.Policies.Current);
            fixture.Inject(PrivacyPolicies.Policies.Current.DataTypes.Set.First().Id);
            fixture.Inject(PrivacyPolicies.Policies.Current.Protocols.Set.First().Id);
            fixture.Inject<IEnumerable<PrivacyPolicies.CapabilityId>>(new[] { PrivacyPolicies.Policies.Current.Capabilities.Ids.Delete });
            fixture.Inject<OptionalFeatureId>(Policies.Current.OptionalFeatures.Ids.MsaAgeOutOptIn);
            fixture.Inject<IEnumerable<PrivacyPolicies.DataTypeId>>(new[] { PrivacyPolicies.Policies.Current.DataTypes.Set.First().Id });
            fixture.Inject<IEnumerable<PrivacyPolicies.SubjectTypeId>>(new[] { PrivacyPolicies.Policies.Current.SubjectTypes.Set.First().Id });
            fixture.Inject<IEnumerable<OptionalFeatureId>>(new[] { Policies.Current.OptionalFeatures.Ids.MsaAgeOutOptIn });
            return fixture;
        }

        /// <summary>
        /// Updates the fixture so that Microsoft.PrivacyServices.Identity data can be mocked.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture EnableIdentity(this IFixture fixture)
        {
            fixture.Inject<IManifest>(Manifest.Current);

            fixture.Customize<AssetQualifier>(
                x =>
                x.FromFactory<int>(i =>
                {
                    var index = i % Manifest.Current.AssetTypes.Count();

                    var typeDefinition = Manifest.Current.AssetTypes.ElementAt(index);

                    var dictionary = new Dictionary<string, string>();
                    dictionary.Add("AssetType", typeDefinition.Id.ToString());

                    foreach (var prop in typeDefinition.Properties)
                    {
                        if (prop.Id == "RelativePath")
                        {
                            dictionary.Add(prop.Id, $"/local/{fixture.Create<string>()}");
                        }
                        else
                        {
                            dictionary.Add(prop.Id, fixture.Create<string>());
                        }
                    }

                    return AssetQualifier.CreateFromDictionary(dictionary);
                }));

            return fixture;
        }

    }
}
