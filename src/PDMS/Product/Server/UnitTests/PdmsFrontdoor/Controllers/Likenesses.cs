namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;
    using SemanticComparison;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    public static class Likenesses
    {
        public static Likeness<DataOwner, Core.DataOwner> ForDataOwner(DataOwner testDataOwner)
        {
            return testDataOwner.Likeness<DataOwner, Core.DataOwner>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(v => v.WriteSecurityGroups).EqualsWhen((src, dest) =>
                    src.WriteSecurityGroups.SequenceLike(dest.WriteSecurityGroups))

                .With(v => v.TagSecurityGroups).EqualsWhen((src, dest) =>
                    src.TagSecurityGroups.SequenceLike(dest.TagSecurityGroups))

                .With(v => v.TagApplicationIds).EqualsWhen((src, dest) =>
                    src.TagApplicationIds.SequenceLike(dest.TagApplicationIds))

                .With(v => v.AlertContacts).EqualsWhen((src, dest) =>
                    src.AlertContacts.SequenceLike<string>(dest.AlertContacts.Select(m => m.Address)))

                .With(v => v.AnnouncementContacts).EqualsWhen((src, dest) =>
                    src.AnnouncementContacts.SequenceLike<string>(dest.AnnouncementContacts.Select(m => m.Address)))

                .With(v => v.SharingRequestContacts).EqualsWhen((src, dest) =>
                    src.SharingRequestContacts.SequenceLike<string>(dest.SharingRequestContacts.Select(m => m.Address)))

                .With(v => v.DataAgents).EqualsWhen((src, dest) =>
                    src.DataAgents.SequenceLike(dest.DataAgents))

                .With(v => v.AssetGroups).EqualsWhen((src, dest) =>
                    src.AssetGroups.SequenceLike(dest.AssetGroups))

                .With(v => v.ServiceTree).EqualsWhen((src, dest) =>
                    ForServiceTree(src.ServiceTree).ShouldEqual_(dest.ServiceTree))

                .With(v => v.Icm).EqualsWhen((src, dest) =>
                    src.Icm?.ConnectorId.ToString() == dest.Icm?.ConnectorId.ToString())

                .Without(v => v.EntityType)
                .Without(v => v.HasInitiatedTransferRequests)
                .Without(v => v.HasPendingTransferRequests)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<ServiceTree, Core.ServiceTree> ForServiceTree(ServiceTree testServiceTree)
        {
            return testServiceTree.Likeness<ServiceTree, Core.ServiceTree>()
                .With(v => v.ServiceAdmins).EqualsWhen((src, dest) =>
                    src.ServiceAdmins.SequenceLike<string>(dest.ServiceAdmins))
                .With(v => v.Level).EqualsWhen((src, dest) =>
                    src.Level.ToString().Equals(dest.Level.ToString()));
        }

        public static Likeness<AssetGroup, Core.AssetGroup> ForAssetGroup(AssetGroup testAssetGroup)
        {
            return testAssetGroup.Likeness<AssetGroup, Core.AssetGroup>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.OwnerId).EqualsWhen((src, dest) =>
                    src.OwnerId.Equals(dest.OwnerId.ToString()))

                .With(v => v.DeleteAgentId).EqualsWhen((src, dest) =>
                    src.DeleteAgentId.Equals(dest.DeleteAgentId.ToString()))

                .With(v => v.ExportAgentId).EqualsWhen((src, dest) =>
                    src.ExportAgentId.Equals(dest.ExportAgentId.ToString()))

                .With(v => v.AccountCloseAgentId).EqualsWhen((src, dest) =>
                    src.AccountCloseAgentId.Equals(dest.AccountCloseAgentId.ToString()))

                .With(v => v.InventoryId).EqualsWhen((src, dest) =>
                    src.InventoryId.Equals(dest.InventoryId.ToString()))

                .With(v => v.DeleteSharingRequestId).EqualsWhen((src, dest) =>
                    src.DeleteSharingRequestId.Equals(dest.DeleteSharingRequestId.ToString()))

                .With(v => v.ExportSharingRequestId).EqualsWhen((src, dest) =>
                    src.ExportSharingRequestId.Equals(dest.ExportSharingRequestId.ToString()))

                .With(v => v.ComplianceState).EqualsWhen((src, dest) =>
                    ForComplianceState(src.ComplianceState).ShouldEqual_(dest.ComplianceState))

                .With(v => v.Qualifier).EqualsWhen((src, dest) => AreEquivalent(src.Qualifier, dest.Qualifier))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(v => v.DataAssets).EqualsWhen((src, dest) =>
                    src.DataAssets.SequenceLike(dest.DataAssets))

                .With(v => v.Variants).EqualsWhen((src, dest) =>
                    src.Variants.SequenceLike(dest.Variants, ForAssetGroupVariant))

                .With(v => v.DeleteAgent).EqualsWhen((src, dest) =>
                    src.DeleteAgent.LikenessShouldEqual(dest.DeleteAgent))

                .With(v => v.ExportAgent).EqualsWhen((src, dest) =>
                    src.ExportAgent.LikenessShouldEqual(dest.ExportAgent))

                .With(v => v.AccountCloseAgent).EqualsWhen((src, dest) =>
                    src.AccountCloseAgent.LikenessShouldEqual(dest.AccountCloseAgent))

                .With(v => v.Inventory).EqualsWhen((src, dest) =>
                    src.Inventory.LikenessShouldEqual(dest.Inventory))

                .With(v => v.Owner).EqualsWhen((src, dest) =>
                    src.Owner.LikenessShouldEqual(dest.Owner))

                .Without(v => v.OptionalFeatures)
                .Without(v => v.EntityType)
                .Without(v => v.QualifierParts)
                .Without(v => v.HasPendingTransferRequest)
                .Without(v => v.PendingTransferRequestTargetOwnerId)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<Inventory, Core.Inventory> ForInventory(Inventory inventory)
        {
            return inventory.Likeness<Inventory, Core.Inventory>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(v => v.DataCategory).EqualsWhen((src, dest) =>
                    src.DataCategory.ToString().Equals(dest.DataCategory.ToString()))

                .With(v => v.RetentionPolicy).EqualsWhen((src, dest) =>
                    src.RetentionPolicy.ToString().Equals(dest.RetentionPolicy.ToString()))

                .With(v => v.DisposalMethod).EqualsWhen((src, dest) =>
                    src.DisposalMethod.ToString().Equals(dest.DisposalMethod.ToString()))

                .With(v => v.DocumentationType).EqualsWhen((src, dest) =>
                    src.DocumentationType.ToString().Equals(dest.DocumentationType.ToString()))

                .With(v => v.DocumentationLink).EqualsWhen((src, dest) =>
                    src.DocumentationLink.Equals(dest.DocumentationLink.ToString()))

                .With(v => v.ThirdPartyRelation).EqualsWhen((src, dest) =>
                    src.ThirdPartyRelation.ToString().Equals(dest.ThirdPartyRelation.ToString()))

                .With(v => v.OwnerId).EqualsWhen((src, dest) =>
                    src.OwnerId.Equals(dest.OwnerId.ToString()))

                .With(v => v.Owner).EqualsWhen((src, dest) =>
                    src.Owner.LikenessShouldEqual(dest.Owner))

                .Without(v => v.EntityType)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<VariantDefinition, Core.VariantDefinition> ForVariantDefinition(VariantDefinition variantDefinition)
        {
            return variantDefinition.Likeness<VariantDefinition, Core.VariantDefinition>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(v => v.DataTypes).EqualsWhen((src, dest) =>
                    src.DataTypes.SequenceLike<string>(dest.DataTypes.Select(m => m.Value)))

                .With(v => v.Capabilities).EqualsWhen((src, dest) =>
                    src.Capabilities.SequenceLike<string>(dest.Capabilities.Select(m => m.Value)))

                .With(v => v.SubjectTypes).EqualsWhen((src, dest) =>
                    src.SubjectTypes.SequenceLike<string>(dest.SubjectTypes.Select(m => m.Value)))

                .With(v => v.OwnerId).EqualsWhen((src, dest) =>
                    src.OwnerId.Equals(dest.OwnerId.ToString()))

                .With(v => v.Owner).EqualsWhen((src, dest) =>
                    src.Owner.LikenessShouldEqual(dest.Owner))

                .With(v => v.State).EqualsWhen((src, dest) =>
                    src.State.LikenessShouldEqual(dest.State))

                .With(v => v.Reason).EqualsWhen((src, dest) =>
                    src.Reason.LikenessShouldEqual(dest.Reason))

                .Without(v => v.EntityType)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<AssetGroupVariant, Core.AssetGroupVariant> ForAssetGroupVariant(AssetGroupVariant assetGroupVariant)
        {
            return assetGroupVariant.Likeness<AssetGroupVariant, Core.AssetGroupVariant>()
                .With(v => v.VariantId).EqualsWhen((src, dest) =>
                    src.VariantId.Equals(dest.VariantId.ToString()))

                .With(v => v.VariantState).EqualsWhen((src, dest) =>
                    src.VariantState.ToString().Equals(dest.VariantState.ToString()))

                .With(v => v.VariantExpiryDate).EqualsWhen((src, dest) =>
                {
                    if (src.VariantExpiryDate == null)
                    {
                        return dest.VariantExpiryDate == null;
                    }
                    else
                    {
                        return src.VariantExpiryDate.Equals(dest.VariantExpiryDate);
                    }
                })

                .With(v => v.TfsTrackingUris).EqualsWhen((src, dest) =>
                    src.TfsTrackingUris.SequenceLike<string>(dest.TfsTrackingUris.Select(m => m.ToString())));
        }

        public static Likeness<ComplianceState, Core.ComplianceState> ForComplianceState(ComplianceState arg)
        {
            return arg.Likeness<ComplianceState, Core.ComplianceState>()
                .With(v => v.IncompliantReason).EqualsWhen((src, dest) =>
                {
                    if (src.IncompliantReason == null)
                    {
                        return dest.IncompliantReason == null;
                    }
                    else
                    {
                        return src.IncompliantReason.ToString().Equals(dest.IncompliantReason.ToString());
                    }
                });
        }

        public static Likeness<DataAgent, Core.DataAgent> ForDataAgent(DataAgent testDataAgent)
        {
            return testDataAgent.Likeness<DataAgent, Core.DataAgent>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.OwnerId).EqualsWhen((src, dest) =>
                    src.OwnerId.Equals(dest.OwnerId.ToString()))

                .With(v => v.ConnectionDetails).EqualsWhen((src, dest) =>
                    src.ConnectionDetails.SequenceLike(dest.ConnectionDetails, ForConnectionDetails))

                .With(v => v.MigratingConnectionDetails).EqualsWhen((src, dest) =>
                    src.MigratingConnectionDetails.SequenceLike(dest.MigratingConnectionDetails, ForConnectionDetails))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(v => v.OperationalReadinessLow).EqualsWhen((src, dest) =>
                    src.OperationalReadinessLow.Equals(dest.OperationalReadinessLow))

                .With(v => v.OperationalReadinessHigh).EqualsWhen((src, dest) =>
                    src.OperationalReadinessHigh.Equals(dest.OperationalReadinessHigh))

                .With(v => v.Icm).EqualsWhen((src, dest) =>
                    src.Icm?.ConnectorId.ToString() == dest.Icm?.ConnectorId.ToString())

                .Without(v => v.Capabilities)
                .Without(v => v.EntityType)
                .Without(v => v.DerivedEntityType)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<ConnectionDetail, KeyValuePair<Core.ReleaseState, Core.ConnectionDetail>> ForConnectionDetails(ConnectionDetail testConnectionDetail)
        {
            return testConnectionDetail.Likeness<ConnectionDetail, KeyValuePair<Core.ReleaseState, Core.ConnectionDetail>>()
                .With(v => v.Key).EqualsWhen((src, dest) =>
                    src.ReleaseState.ToString().Equals(dest.Key.ToString()))

                .With(v => v.Value).EqualsWhen((src, dest) => ForConnectionDetail(src).ShouldEqual_(dest.Value));
        }

        public static Likeness<ConnectionDetail, Core.ConnectionDetail> ForConnectionDetail(ConnectionDetail testConnectionDetail)
        {
            return testConnectionDetail.Likeness<ConnectionDetail, Core.ConnectionDetail>()
                .With(v => v.Protocol).EqualsWhen((src, dest) =>
                    src.Protocol.Equals(dest.Protocol.Value))

                .With(v => v.AuthenticationType).EqualsWhen((src, dest) =>
                    src.AuthenticationType.ToString().Equals(dest.AuthenticationType.ToString()))

                .With(v => v.ReleaseState).EqualsWhen((src, dest) =>
                    src.ReleaseState.ToString().Equals(dest.ReleaseState.ToString()))

                .With(v => v.AgentReadiness).EqualsWhen((src, dest) =>
                    src.AgentReadiness.ToString().Equals(dest.AgentReadiness.ToString()))

                .Without(v => v.AadAppIds);
        }

        public static Likeness<DataAsset, Core.DataAsset> ForDataAsset(DataAsset testDataAsset)
        {
            return testDataAsset.Likeness<DataAsset, Core.DataAsset>()
                .With(v => v.Qualifier).EqualsWhen((src, dest) => AreEquivalent(src.Qualifier, dest.Qualifier))
                .Without(v => v.Tags);
        }

        public static Likeness<VariantRequest, Core.VariantRequest> ForVariantRequest(VariantRequest item)
        {
            return item.Likeness<VariantRequest, Core.VariantRequest>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(x => x.OwnerId).EqualsWhen((src, dest) =>
                    src.OwnerId.Equals(dest.OwnerId.ToString()))

                .With(v => v.RequestedVariants).EqualsWhen((src, dest) =>
                    src.RequestedVariants.SequenceLike(dest.RequestedVariants, ForAssetGroupVariant))

                .With(v => v.VariantRelationships).EqualsWhen((src, dest) =>
                    src.VariantRelationships.SequenceLike(dest.VariantRelationships, ForVariantRelationship))

                .With(v => v.WorkItemUri).EqualsWhen((src, dest) =>
                    src.WorkItemUri.Equals(dest.WorkItemUri.ToString()))

                .Without(v => v.EntityType)
                .Without(v => v.IsDeleted);
        }

        public static Likeness<VariantRelationship, KeyValuePair<Guid, Core.VariantRelationship>> ForVariantRelationship(VariantRelationship testVariantRelationship)
        {
            return testVariantRelationship.Likeness<VariantRelationship, KeyValuePair<Guid, Core.VariantRelationship>>()
                .With(v => v.Key).EqualsWhen((src, dest) =>
                    src.AssetGroupId.Equals(dest.Key.ToString()))

                .With(v => v.Value).EqualsWhen((src, dest) =>
                    src.AssetGroupId.Equals(dest.Value.AssetGroupId.ToString()) && AreEquivalent(src.AssetQualifier, dest.Value.AssetQualifier));
        }

        public static Likeness<HistoryItem, Core.HistoryItem> ForHistoryItem(HistoryItem testHistoryItem)
        {
            return testHistoryItem.Likeness<HistoryItem, Core.HistoryItem>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TransactionId).EqualsWhen((src, dest) =>
                    src.TransactionId.Equals(dest.TransactionId.ToString()))

                .With(v => v.WriteAction).EqualsWhen((src, dest) =>
                    src.WriteAction.ToString().Equals(dest.WriteAction.ToString()))

                .Without(v => v.Entity)
                .Without(v => v.EntityType);
        }

        public static Likeness<SetAgentRelationshipParameters.Action, Core.SetAgentRelationshipParameters.Action> ForSetAgentRelationshipParametersAction(SetAgentRelationshipParameters.Action item)
        {
            return item.Likeness<SetAgentRelationshipParameters.Action, Core.SetAgentRelationshipParameters.Action>()
                .With(v => v.CapabilityId).EqualsWhen((src, dest) =>
                    src.CapabilityId == dest.CapabilityId?.ToString())
                .With(v => v.DeleteAgentId).EqualsWhen((src, dest) =>
                    src.DeleteAgentId == dest.DeleteAgentId?.ToString())
                .With(v => v.Verb).EqualsWhen((src, dest) =>
                    src.Verb.ToString() == dest.Verb.ToString());
        }

        public static Likeness<SetAgentRelationshipParameters.Relationship, Core.SetAgentRelationshipParameters.Relationship> ForSetAgentRelationshipParametersRelationship(SetAgentRelationshipParameters.Relationship item)
        {
            return item.Likeness<SetAgentRelationshipParameters.Relationship, Core.SetAgentRelationshipParameters.Relationship>()
                .With(v => v.AssetGroupId).EqualsWhen((src, dest) =>
                    src.AssetGroupId == dest.AssetGroupId.ToString())
                .With(x => x.Actions).EqualsWhen((source, dest) =>
                     source.Actions.SequenceLike(dest.Actions, Likenesses.ForSetAgentRelationshipParametersAction));
        }

        public static Likeness<SetAgentRelationshipParameters, Core.SetAgentRelationshipParameters> ForSetAgentRelationshipParameters(SetAgentRelationshipParameters item)
        {
            return item.Likeness<SetAgentRelationshipParameters, Core.SetAgentRelationshipParameters>()
                .With(x => x.Relationships).EqualsWhen((source, dest) =>
                     source.Relationships.SequenceLike(dest.Relationships, Likenesses.ForSetAgentRelationshipParametersRelationship));
        }

        public static Likeness<Core.SetAgentRelationshipResponse.CapabilityResult, SetAgentRelationshipResponse.CapabilityResult> ForSetAgentRelationshipResponseCapabilityResult(Core.SetAgentRelationshipResponse.CapabilityResult item)
        {
            return item.Likeness<Core.SetAgentRelationshipResponse.CapabilityResult, SetAgentRelationshipResponse.CapabilityResult>()
                .With(v => v.CapabilityId).EqualsWhen((src, dest) =>
                    src.CapabilityId.Value.Equals(dest.CapabilityId))
                .With(v => v.SharingRequestId).EqualsWhen((src, dest) =>
                    src.SharingRequestId?.ToString() == dest.SharingRequestId)
                .With(v => v.Status).EqualsWhen((src, dest) =>
                    src.Status.ToString() == dest.Status.ToString());
        }

        public static Likeness<Core.SetAgentRelationshipResponse.AssetGroupResult, SetAgentRelationshipResponse.AssetGroupResult> ForSetAgentRelationshipResponseAssetGroupResult(Core.SetAgentRelationshipResponse.AssetGroupResult item)
        {
            return item.Likeness<Core.SetAgentRelationshipResponse.AssetGroupResult, SetAgentRelationshipResponse.AssetGroupResult>()
                .With(v => v.AssetGroupId).EqualsWhen((src, dest) =>
                    src.AssetGroupId.ToString() == dest.AssetGroupId)
                .With(x => x.Capabilities).EqualsWhen((source, dest) =>
                    source.Capabilities.SequenceLike(dest.Capabilities, Likenesses.ForSetAgentRelationshipResponseCapabilityResult));
        }

        public static Likeness<Core.SetAgentRelationshipResponse, SetAgentRelationshipResponse> ForSetAgentRelationshipResponse(Core.SetAgentRelationshipResponse item)
        {
            return item.Likeness<Core.SetAgentRelationshipResponse, SetAgentRelationshipResponse>()
                .With(x => x.Results).EqualsWhen((source, dest) =>
                     source.Results.SequenceLike(dest.Results, Likenesses.ForSetAgentRelationshipResponseAssetGroupResult));
        }

        public static Likeness<TransferRequest, Core.TransferRequest> ForTransferRequest(TransferRequest item)
        {
            return item.Likeness<TransferRequest, Core.TransferRequest>()
                .With(v => v.Id).EqualsWhen((src, dest) =>
                    src.Id.Equals(dest.Id.ToString()))

                .With(v => v.TrackingDetails).EqualsWhen((src, dest) =>
                    src.TrackingDetails.LikenessShouldEqual(dest.TrackingDetails))

                .With(x => x.SourceOwnerId).EqualsWhen((src, dest) =>
                    src.SourceOwnerId.Equals(dest.SourceOwnerId.ToString()))

                .With(x => x.TargetOwnerId).EqualsWhen((src, dest) =>
                    src.TargetOwnerId.Equals(dest.TargetOwnerId.ToString()))

                .With(v => v.AssetGroups).EqualsWhen((src, dest) =>
                    src.AssetGroups.SequenceLike(dest.AssetGroups))

                .Without(v => v.EntityType)
                .Without(v => v.RequestState)
                .Without(v => v.IsDeleted);
        }

        public static bool AreEquivalent(string src, AssetQualifier dest)
        {
            var srcCopy = AssetQualifier.Parse(src);
            var destCopy = AssetQualifier.Parse(dest.Value);

            return srcCopy.Value.Equals(destCopy.Value);
        }
    }
}