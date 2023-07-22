using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using Microsoft.Osgs.Core.Helpers;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PdmsClientExceptionHandler))]
    public class RegistrationStatusApiController : Controller
    {
        private readonly IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor;
        private readonly IDataOwnerNameCache ownerNameCache;

        public RegistrationStatusApiController(
            IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor,
            IDataOwnerNameCache ownerNameCache)
        {
            this.pdmsClientProviderAccessor = pdmsClientProviderAccessor ?? throw new ArgumentNullException(nameof(pdmsClientProviderAccessor));
            this.ownerNameCache = ownerNameCache ?? throw new ArgumentNullException(nameof(ownerNameCache));
        }

        [HttpGet]
        public async Task<IActionResult> GetAgentStatus(string agentId)
        {
            EnsureArgument.NotNullOrWhiteSpace(agentId, nameof(agentId));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.CalculateDeleteAgentRegistrationStatus(agentId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            var agentOwnerId = result.OwnerId;

            result.AssetGroups = result.AssetGroups.OrderBy(ShowOwnerAssetGroupsFirst).ThenBy(ag => ag.OwnerId);

            int ShowOwnerAssetGroupsFirst(PdmsApiModelsV2.AssetGroupRegistrationStatus assetGroupRegistrationStatus)
            {
               return assetGroupRegistrationStatus.OwnerId == agentOwnerId ? 0 : 1;
            }

            var ownerIds = result.AssetGroups.Select(ag => ag.OwnerId);
            var ownerNames = await ownerNameCache.GetDataOwnerNamesAsync(ownerIds);
            return Json(RegistrationStatusConverters.ToAgentRegistrationStatusModelWithNames(result, ownerNames));
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupStatus(string assetGroupId)
        {
            EnsureArgument.NotNullOrWhiteSpace(assetGroupId, nameof(assetGroupId));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.CalculateRegistrationStatus(assetGroupId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return Json(RegistrationStatusConverters.ToAssetGroupRegistrationStatusModel(result));
        }

    }
}
