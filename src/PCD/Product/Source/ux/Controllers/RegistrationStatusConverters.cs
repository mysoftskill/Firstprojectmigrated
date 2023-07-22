using System.Collections.Generic;
using System.Linq;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms.RegistrationStatus;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    // TODO:  move this to Microsoft.PrivacyServices.UX.Core.PdmsClient.Converters
    public static class RegistrationStatusConverters
    {
        public static PdmsModels.AgentRegistrationStatus ToAgentRegistrationStatusModel(PdmsApiModelsV2.AgentRegistrationStatus agentStatus) => new PdmsModels.AgentRegistrationStatus()
        {
            OwnerId = agentStatus.OwnerId,
            IsComplete = agentStatus.IsComplete,
            Environments = agentStatus.Environments,
            EnvironmentStatus = agentStatus.EnvironmentStatus,
            Protocols = agentStatus.Protocols,
            ProtocolStatus = agentStatus.ProtocolStatus,
            Capabilities = agentStatus.Capabilities,
            CapabilityStatus = agentStatus.CapabilityStatus,
            AssetGroups = agentStatus.AssetGroups.Select(ag => ToAssetGroupRegistrationStatusModel(ag)),
            AssetGroupsStatus = agentStatus.AssetGroupsStatus,
        };

        public static PdmsModels.AgentRegistrationStatus ToAgentRegistrationStatusModelWithNames(PdmsApiModelsV2.AgentRegistrationStatus agentStatus, IReadOnlyDictionary<string, string> ownerNames)
        {
            var agentRegistrationStatusModel = RegistrationStatusConverters.ToAgentRegistrationStatusModel(agentStatus);
            agentRegistrationStatusModel.AssetGroups = agentRegistrationStatusModel.AssetGroups.Select(ag =>
            {
                ag.OwnerName = (ag.OwnerId != null && ownerNames.ContainsKey(ag.OwnerId)) ? ownerNames[ag.OwnerId] : ag.OwnerId;
                return ag;
            });
            return agentRegistrationStatusModel;
        }

        public static PdmsModels.AssetGroupRegistrationStatus ToAssetGroupRegistrationStatusModel(PdmsApiModelsV2.AssetGroupRegistrationStatus assetGroupStatus) => new PdmsModels.AssetGroupRegistrationStatus()
        {
            Id = assetGroupStatus.Id,
            OwnerId = assetGroupStatus.OwnerId,
            IsComplete = assetGroupStatus.IsComplete,
            Assets = assetGroupStatus.Assets.Select(a => ToAssetGroupRegistrationStatusModel(a)),
            AssetsStatus = assetGroupStatus.AssetsStatus,
            Qualifier = ApiController.ConvertToAssetGroupQualifierModel(assetGroupStatus.Qualifier)
        };

        public static PdmsModels.AssetRegistrationStatus ToAssetGroupRegistrationStatusModel(PdmsApiModelsV2.AssetRegistrationStatus assetStatus) => new PdmsModels.AssetRegistrationStatus()
        {
            DataTypeTags = assetStatus.DataTypeTags,
            DataTypeTagsStatus = assetStatus.DataTypeTagsStatus,
            Id = assetStatus.Id,
            IsComplete = assetStatus.IsComplete,
            IsLongTailOrCustomNonUse = assetStatus.IsLongTailOrCustomNonUse,
            Qualifier = ApiController.ConvertToAssetGroupQualifierModel(assetStatus.Qualifier),
            IsNonPersonal = assetStatus.IsNonPersonal,
            SubjectTypeTags = assetStatus.SubjectTypeTags,
            SubjectTypeTagsStatus = assetStatus.SubjectTypeTagsStatus
        };
    }
}
