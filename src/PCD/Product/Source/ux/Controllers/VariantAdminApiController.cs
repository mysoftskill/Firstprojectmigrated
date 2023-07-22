using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [Authorize("VariantAdmin")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PdmsClientExceptionHandler))]
    public class VariantAdminApiController : Controller
    {
        private readonly IPdmsClientProvider pdmsClient;

        private readonly IVariantNameCache variantNameCache;

        public VariantAdminApiController(
            IPdmsClientProvider pdmsClient,
            IVariantNameCache variantNameCache)
        {
            this.pdmsClient = pdmsClient ?? throw new ArgumentNullException(nameof(pdmsClient));
            this.variantNameCache = variantNameCache ?? throw new ArgumentNullException(nameof(variantNameCache));
        }

        /// <summary>
        /// This method determines if user has access for variant admin requests.
        /// Returns 200 when "VariantAdmin" claim is found on user ticket, otherwise 403.
        /// </summary>
        [HttpGet]
        public void HasAccess()
        {
            // TODO Bug 15983410: This should not be called after the claim exists for the authenticated user.
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVariantRequests()
        {
            var allVariantRequests = (await pdmsClient.Instance.VariantRequests.ReadAllByFiltersAsync(await pdmsClient.CreateNewRequestContext())).Response;

            var variantIds = allVariantRequests.SelectMany(r => r.RequestedVariants.Select(rv => rv.VariantId));
            var variantNames = await variantNameCache.GetVariantNamesAsync(variantIds);

            var variantRequests = allVariantRequests
                .OrderBy(r => r.OwnerName)
                .Select(r => VariantConverters.ToVariantRequestModelWithNames(r, variantNames));

            return Json(variantRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ApproveVariantRequest(string variantRequestId)
        {
            EnsureArgument.NotNullOrWhiteSpace(variantRequestId, nameof(variantRequestId));

            var variantRequest = (await pdmsClient.Instance.VariantRequests.ReadAsync(variantRequestId, await pdmsClient.CreateNewRequestContext())).Response;

            await pdmsClient.Instance.VariantRequests.ApproveAsync(variantRequestId, variantRequest.ETag, await pdmsClient.CreateNewRequestContext());
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DenyVariantRequest(string variantRequestId)
        {
            EnsureArgument.NotNullOrWhiteSpace(variantRequestId, nameof(variantRequestId));

            var variantRequest = (await pdmsClient.Instance.VariantRequests.ReadAsync(variantRequestId, await pdmsClient.CreateNewRequestContext())).Response;

            await pdmsClient.Instance.VariantRequests.DeleteAsync(variantRequestId, variantRequest.ETag, await pdmsClient.CreateNewRequestContext());
        }
    }
}
