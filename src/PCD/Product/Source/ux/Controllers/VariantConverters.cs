using System.Collections.Generic;
using System.Linq;
using Microsoft.Osgs.Core.Helpers;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    // TODO:  move this to Microsoft.PrivacyServices.UX.Core.PdmsClient.Converters
    public static class VariantConverters
    {
        public static PdmsModels.VariantDefinition ToVariantDefinitionModel(PdmsApiModelsV2.VariantDefinition variantDefinition) => new PdmsModels.VariantDefinition()
        {
            Id = variantDefinition.Id,
            Name = variantDefinition.Name,
            Description = variantDefinition.Description,
            OwnerId = variantDefinition.OwnerId,
            Approver = variantDefinition.Approver,
            Capabilities = variantDefinition.Capabilities.Select(c => c.Value),
            DataTypes = variantDefinition.DataTypes.Select(dt => dt.Value),
            SubjectTypes = variantDefinition.SubjectTypes.Select(st => st.Value),
        };

        public static PdmsModels.VariantRequest ToVariantRequestModel(PdmsApiModelsV2.VariantRequest variantRequest) => new PdmsModels.VariantRequest()
        {
            Id = variantRequest.Id,
            TrackingDetails = variantRequest.TrackingDetails != null ? ApiController.ConvertToTrackingDetails(variantRequest.TrackingDetails) : null,
            OwnerId = variantRequest.OwnerId,
            OwnerName = variantRequest.OwnerName,
            RequestedVariants = variantRequest.RequestedVariants.Select(rv => ToAssetGroupVariantModel(rv)),
            VariantRelationships = variantRequest.VariantRelationships.Select(vr => ToVariantRelationshipModel(vr))
        };

        public static PdmsModels.VariantRequest ToVariantRequestModelWithNames(PdmsApiModelsV2.VariantRequest variantRequest, IReadOnlyDictionary<string, string> variantNames) 
        {
            EnsureArgument.NotNull(variantRequest, nameof(variantRequest));
            EnsureArgument.NotNull(variantNames, nameof(variantNames));

            var variantRequestModel = VariantConverters.ToVariantRequestModel(variantRequest);
            variantRequestModel.RequestedVariants = variantRequestModel.RequestedVariants.Select(rv =>
            {
                rv.VariantName = variantNames.ContainsKey(rv.VariantId) ? variantNames[rv.VariantId] : rv.VariantId;
                return rv;
            });
            if (variantRequest.WorkItemUri != null)
            {
                variantRequestModel.RequestedVariants = variantRequestModel.RequestedVariants.Select(rv =>
                {
                    rv.TfsTrackingUris = rv.TfsTrackingUris.Append(variantRequest.WorkItemUri.ToString());
                    return rv;
                });
            }
            return variantRequestModel;
        }

        public static PdmsApiModelsV2.VariantRequest ToVariantRequest(PdmsModels.VariantRequest variantRequest) => new PdmsApiModelsV2.VariantRequest()
        {
            Id = variantRequest.Id,
            OwnerId = variantRequest.OwnerId,
            OwnerName = variantRequest.OwnerName,
            RequestedVariants = variantRequest.RequestedVariants.Select(rv => ToAssetGroupVariant(rv)),
            VariantRelationships = variantRequest.VariantRelationships.Select(vr =>
            new PdmsApiModelsV2.VariantRelationship
            {
                AssetGroupId = vr.AssetGroupId
            })
        };

        public static PdmsModels.AssetGroupVariant ToAssetGroupVariantModel(PdmsApiModelsV2.AssetGroupVariant assetGroupVariant) => new PdmsModels.AssetGroupVariant()
        {
            VariantId = assetGroupVariant.VariantId,
            TfsTrackingUris = assetGroupVariant.TfsTrackingUris,
            DisabledSignalFiltering = assetGroupVariant.DisableSignalFiltering,
            VariantExpiryDate = assetGroupVariant.VariantExpiryDate,
            VariantState = assetGroupVariant.VariantState
        };

        public static PdmsApiModelsV2.AssetGroupVariant ToAssetGroupVariant(PdmsModels.AssetGroupVariant assetGroupVariant) => new PdmsApiModelsV2.AssetGroupVariant()
        {
            VariantId = assetGroupVariant.VariantId,
            TfsTrackingUris = assetGroupVariant.TfsTrackingUris,
            DisableSignalFiltering = assetGroupVariant.DisabledSignalFiltering,
            VariantExpiryDate = assetGroupVariant.VariantExpiryDate,
            VariantState = assetGroupVariant.VariantState
        };

        public static PdmsModels.VariantRelationship ToVariantRelationshipModel(PdmsApiModelsV2.VariantRelationship variantRelationship) => new PdmsModels.VariantRelationship()
        {
            AssetGroupId = variantRelationship.AssetGroupId,
            AssetGroupQualifier = ApiController.ConvertToAssetGroupQualifierModel(variantRelationship.AssetQualifier)
        };
    }
}
