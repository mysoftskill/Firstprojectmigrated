using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PdmsClientExceptionHandler))]
    public class VariantApiController : Controller
    {
        // TODO: Remove this when all IPdmsClientProvider methods are mocked and exposed via IClientProviderAccessor
        private readonly IPdmsClientProvider pdmsClient;

        private readonly IVariantNameCache variantNameCache;

        public VariantApiController(
            IPdmsClientProvider pdmsClient, 
            IVariantNameCache variantNameCache)
        {
            this.pdmsClient = pdmsClient ?? throw new ArgumentNullException(nameof(pdmsClient));
            this.variantNameCache = variantNameCache ?? throw new ArgumentNullException(nameof(variantNameCache));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task CreateVariantRequest([FromBody] PdmsModels.VariantRequest request)
        {
            await pdmsClient.Instance.VariantRequests.CreateAsync(
                VariantConverters.ToVariantRequest(request),
                await pdmsClient.CreateNewRequestContext()
            );
        }

        [HttpGet]
        public async Task<IActionResult> GetVariants()
        {
            var result = (await pdmsClient.Instance.VariantDefinitions.ReadAllByFiltersAsync(await pdmsClient.CreateNewRequestContext())).Response;

            return Json(result.OrderBy(v => v.Name).Select(variant => VariantConverters.ToVariantDefinitionModel(variant)));
        }

        [HttpGet]
        public async Task<IActionResult> GetVariantRequestsByOwnerId(string ownerId, string assetGroupId = null)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.VariantRequestFilterCriteria()
            {
                OwnerId = ownerId
            };

            var result = (await pdmsClient.Instance.VariantRequests.ReadAllByFiltersAsync(
                await pdmsClient.CreateNewRequestContext(),
                PdmsApiModelsV2.VariantRequestExpandOptions.None,
                filter
            )).Response;

            if (assetGroupId != null)
            {
                result = result.Where(sr => sr.VariantRelationships.Any(relationship => relationship.AssetGroupId.Equals(assetGroupId)));
            }

            var variantIds = result.SelectMany(v => v.RequestedVariants.Select(rv => rv.VariantId));
            var variantNames = await variantNameCache.GetVariantNamesAsync(variantIds);


            return Json(result.Select(sr => VariantConverters.ToVariantRequestModelWithNames(sr, variantNames)));
        }

        [HttpGet]
        public async Task<IActionResult> GetVariantRequestById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var variantRequest = (await pdmsClient.Instance.VariantRequests.ReadAsync(id, await pdmsClient.CreateNewRequestContext())).Response;

            var variantIds = variantRequest.RequestedVariants.Select(rv => rv.VariantId);
            var variantNames = await variantNameCache.GetVariantNamesAsync(variantIds);

            return Json(VariantConverters.ToVariantRequestModelWithNames(variantRequest, variantNames));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DeleteVariantRequestById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var variantRequest = (await pdmsClient.Instance.VariantRequests.ReadAsync(id, await pdmsClient.CreateNewRequestContext())).Response;

            await pdmsClient.Instance.VariantRequests.DeleteAsync(id, variantRequest.ETag, await pdmsClient.CreateNewRequestContext());
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkVariant(string assetGroupId, string variantId, string eTag)
        {
            EnsureArgument.NotNullOrWhiteSpace(assetGroupId, nameof(assetGroupId));
            EnsureArgument.NotNullOrWhiteSpace(variantId, nameof(variantId));
            EnsureArgument.NotNullOrWhiteSpace(eTag, nameof(eTag));

            var assetGroup = await pdmsClient.Instance.AssetGroups.RemoveVariantsAsync(assetGroupId, new[] { variantId }, eTag, await pdmsClient.CreateNewRequestContext());
            return Json(ApiController.ConvertToAssetGroupModel(assetGroup.Response));
        }
    }
}
