using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PdmsClientExceptionHandler))]
    public class AssetTransferRequestApiController : Controller
    {
        private readonly IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor;

        public AssetTransferRequestApiController(
            IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor)
        {
            this.pdmsClientProviderAccessor = pdmsClientProviderAccessor ?? throw new ArgumentNullException(nameof(pdmsClientProviderAccessor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task CreateTransferRequest([FromBody] PdmsModels.TransferRequest transferRequest)
        {
            var request = ConvertToTransferRequestModel(transferRequest);

            await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.CreateAsync(
                request, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        [HttpGet]
        public async Task<IActionResult> GetTransferRequestsByTargetOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.TransferRequestFilterCriteria()
            {
                TargetOwnerId = ownerId,
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.TransferRequestExpandOptions.None,
                filter
            )).Response;

            var sourceOwners = (await GetOwners(result.Select(request => request.SourceOwnerId).Distinct())).ToDictionary(o => o.Id, o => o.Name);
            
            var convertedResult = await Task.WhenAll(result.Select(async sr =>
                ConvertToTransferRequest(
                    sr,
                    await GetAssetGroups(sr.AssetGroups),
                    sourceOwners[sr.SourceOwnerId])
            ));

            return Json(convertedResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ApproveTransferRequests([FromBody] string[] transferRequestIds)
        {
            await Task.WhenAll(transferRequestIds.Select(ApproveTransferRequest));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DenyTransferRequests(string[] transferRequestIds)
        {
            await Task.WhenAll(transferRequestIds.Select(DenyTransferRequest));
        }

        private async Task<PdmsApiModelsV2.DataOwner[]> GetOwners(IEnumerable<string> ownerIds)
        {
            return await Task.WhenAll(ownerIds.Select(async id => await GetOwner(id)));
        }

        private async Task<PdmsApiModelsV2.DataOwner> GetOwner(string id)
        {
            return (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                    id,
                    await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                    PdmsApiModelsV2.DataOwnerExpandOptions.ServiceTree)).Response;
        }

        private async Task<IHttpResult> ApproveTransferRequest(string transferRequestId)
        {
            var pdmsTransferRequest = (await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.ReadAsync(
                transferRequestId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.ApproveAsync(
                transferRequestId, pdmsTransferRequest.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        private async Task<IHttpResult> DenyTransferRequest(string transferRequestId)
        {
            var pdmsTransferRequest = (await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.ReadAsync(
                transferRequestId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return await pdmsClientProviderAccessor.ProviderInstance.Instance.TransferRequests.DeleteAsync(
                transferRequestId, pdmsTransferRequest.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        private async Task<PdmsApiModelsV2.AssetGroup[]> GetAssetGroups(IEnumerable<string> assetGroupIds)
        {
            return await Task.WhenAll(assetGroupIds.Select(async id =>
            (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAsync(
                id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response));
        }

        private PdmsModels.TransferRequest ConvertToTransferRequest(
            PdmsApiModelsV2.TransferRequest transferRequest,
            PdmsApiModelsV2.AssetGroup[] assetGroups,
            string sourceOwnerName) => new PdmsModels.TransferRequest()
        {
            Id = transferRequest.Id,
            SourceOwnerId = transferRequest.SourceOwnerId,
            SourceOwnerName = sourceOwnerName,
            TargetOwnerId = transferRequest.TargetOwnerId,
            RequestState = ConvertToTransferRequestStates(transferRequest.RequestState),
            AssetGroups = assetGroups.Select(assetGroup => ApiController.ConvertToAssetGroupModel(assetGroup))
        };

        private PdmsModels.TransferRequestStates ConvertToTransferRequestStates(PdmsApiModelsV2.TransferRequestStates transferRequestState)
        {
            switch (transferRequestState)
            {
                case PdmsApiModelsV2.TransferRequestStates.None:
                    return PdmsModels.TransferRequestStates.None;

                case PdmsApiModelsV2.TransferRequestStates.Pending:
                    return PdmsModels.TransferRequestStates.Pending;

                case PdmsApiModelsV2.TransferRequestStates.Approved:
                    return PdmsModels.TransferRequestStates.Approved;

                case PdmsApiModelsV2.TransferRequestStates.Approving:
                    return PdmsModels.TransferRequestStates.Approving;

                case PdmsApiModelsV2.TransferRequestStates.Cancelled:
                    return PdmsModels.TransferRequestStates.Cancelled;

                case PdmsApiModelsV2.TransferRequestStates.Failed:
                    return PdmsModels.TransferRequestStates.Failed;

                default:
                    throw new NotSupportedException($"Service tree level {transferRequestState} is not supported.");
            }
        }

        private PdmsApiModelsV2.TransferRequest ConvertToTransferRequestModel(PdmsModels.TransferRequest transferRequest) =>
            new PdmsApiModelsV2.TransferRequest()
        {
            Id = transferRequest.Id,
            SourceOwnerId = transferRequest.SourceOwnerId,
            TargetOwnerId = transferRequest.TargetOwnerId,
            RequestState = ConvertToTransferRequestStatesModel(transferRequest.RequestState),
            AssetGroups = transferRequest.AssetGroups.Select(assetGroup => assetGroup.Id)
        };

        private PdmsApiModelsV2.TransferRequestStates ConvertToTransferRequestStatesModel(PdmsModels.TransferRequestStates transferRequestState)
        {
            switch (transferRequestState)
            {
                case PdmsModels.TransferRequestStates.None:
                    return PdmsApiModelsV2.TransferRequestStates.None;

                case PdmsModels.TransferRequestStates.Pending:
                    return PdmsApiModelsV2.TransferRequestStates.Pending;

                case PdmsModels.TransferRequestStates.Approved:
                    return PdmsApiModelsV2.TransferRequestStates.Approved;

                case PdmsModels.TransferRequestStates.Approving:
                    return PdmsApiModelsV2.TransferRequestStates.Approving;

                case PdmsModels.TransferRequestStates.Cancelled:
                    return PdmsApiModelsV2.TransferRequestStates.Cancelled;

                case PdmsModels.TransferRequestStates.Failed:
                    return PdmsApiModelsV2.TransferRequestStates.Failed;

                default:
                    throw new NotSupportedException($"Transfer request state {transferRequestState} is not supported.");
            }
        }
    }
}
