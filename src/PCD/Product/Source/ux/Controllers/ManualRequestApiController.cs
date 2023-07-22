using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.PxsClient;
using Pxs = Microsoft.PrivacyServices.PrivacyOperation;
using PxsApiModels = Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
using PxsModels = Microsoft.PrivacyServices.UX.Models.Pxs;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [Authorize("ManualRequests")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PxsClientExceptionHandler))]
    public class ManualRequestApiController : Controller
    {
        private readonly IClientProviderAccessor<IPxsClientProvider> pxsClientProviderAccessor;

        private readonly IPxsClientConfig pxsConfig;

        private readonly IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor;

        private const int GetDataTypesOnSubjectRequestCacheDuration = 60 * 60 * 24 * 14; /* Two weeks, in seconds */

        #region Manual Request Data Types

        private readonly IEnumerable<string> DemographicDeleteDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.CustomerContact.Value,
            Policy.Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value,
            Policy.Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policy.Policies.Current.DataTypes.Ids.FitnessAndActivity.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportContent.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportInteraction.Value,
        };

        private readonly IEnumerable<string> MicrosoftEmployeeDeleteDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.FitnessAndActivity.Value,
            Policy.Policies.Current.DataTypes.Ids.ProfessionalAndPersonalProfile.Value,
            Policy.Policies.Current.DataTypes.Ids.LearningAndDevelopment.Value,
            Policy.Policies.Current.DataTypes.Ids.Recruitment.Value,
            Policy.Policies.Current.DataTypes.Ids.WorkplaceInteractions.Value,
        };

        private readonly IEnumerable<string> MsaSelfAuthDeleteDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
            Policy.Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value,
            Policy.Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
            Policy.Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value,
            Policy.Policies.Current.DataTypes.Ids.ContentConsumption.Value,
            Policy.Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value,
            Policy.Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory.Value,
            Policy.Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration.Value,
            Policy.Policies.Current.DataTypes.Ids.DemographicInformation.Value,
            Policy.Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policy.Policies.Current.DataTypes.Ids.FitnessAndActivity.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportContent.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportInteraction.Value,
            Policy.Policies.Current.DataTypes.Ids.EnvironmentalSensor.Value,
        };

        private readonly IEnumerable<string> DemographicExportDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.CustomerContact.Value,
            Policy.Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value,
            Policy.Policies.Current.DataTypes.Ids.DemographicInformation.Value,
            Policy.Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportContent.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportInteraction.Value,
        };

        private readonly IEnumerable<string> MicrosoftEmployeeExportDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
            Policy.Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration.Value,
            Policy.Policies.Current.DataTypes.Ids.DemographicInformation.Value,
            Policy.Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policy.Policies.Current.DataTypes.Ids.CompensationAndBenefits.Value,
            Policy.Policies.Current.DataTypes.Ids.ProfessionalAndPersonalProfile.Value,
            Policy.Policies.Current.DataTypes.Ids.LearningAndDevelopment.Value,
            Policy.Policies.Current.DataTypes.Ids.Recruitment.Value,
            Policy.Policies.Current.DataTypes.Ids.WorkProfile.Value,
            Policy.Policies.Current.DataTypes.Ids.WorkRecognition.Value,
        };

        private readonly IEnumerable<string> MsaSelfAuthExportDataTypes = new List<string>()
        {
            Policy.Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value,
            Policy.Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
            Policy.Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value,
            Policy.Policies.Current.DataTypes.Ids.ContentConsumption.Value,
            Policy.Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value,
            Policy.Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory.Value,
            Policy.Policies.Current.DataTypes.Ids.DemographicInformation.Value,
            Policy.Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportContent.Value,
            Policy.Policies.Current.DataTypes.Ids.SupportInteraction.Value,
            Policy.Policies.Current.DataTypes.Ids.EnvironmentalSensor.Value,
            Policy.Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value,
        };

        #endregion

        public ManualRequestApiController(
            IClientProviderAccessor<IPxsClientProvider> pxsClientProviderAccessor, 
            IPxsClientConfig pxsConfig, 
            IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor)
        {
            this.pxsClientProviderAccessor = pxsClientProviderAccessor ?? throw new ArgumentNullException(nameof(pxsClientProviderAccessor));
            this.pxsConfig = pxsConfig ?? throw new ArgumentNullException(nameof(pxsConfig));
            this.instrumentedRequestContextAccessor = instrumentedRequestContextAccessor ?? throw new ArgumentNullException(nameof(instrumentedRequestContextAccessor));
        }

        /// <summary>
        /// This method determines if user has access for manual requests.
        /// Returns 200 when "ManualRequests" claim is found on user ticket, otherwise 403.
        /// </summary>
        [HttpGet]
        public void HasAccess()
        {
            // TODO Bug 15983410: This should not be called after the claim exists for the authenticated user.
        }

        [HttpGet]
        [ResponseCache(Duration = GetDataTypesOnSubjectRequestCacheDuration)]
        public IActionResult GetDeleteDataTypesOnSubjectRequests()
        {
            return Json(new PxsModels.DataTypesOnSubjectRequests()
            {
                DemographicSubject = DemographicDeleteDataTypes,
                MicrosoftEmployeeSubject = MicrosoftEmployeeDeleteDataTypes,
                MsaSelfAuthSubject = MsaSelfAuthDeleteDataTypes
            });
        }

        [HttpGet]
        [ResponseCache(Duration = GetDataTypesOnSubjectRequestCacheDuration)]
        public IActionResult GetExportDataTypesOnSubjectRequests()
        {
            return Json(new PxsModels.DataTypesOnSubjectRequests()
            {
                DemographicSubject = DemographicExportDataTypes,
                MicrosoftEmployeeSubject = MicrosoftEmployeeExportDataTypes,
                MsaSelfAuthSubject = MsaSelfAuthExportDataTypes
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestStatuses(Pxs.Contracts.PrivacyRequestType type)
        {
            var types = new List<Pxs.Contracts.PrivacyRequestType>() { type };

            var filterArgs = new PxsApiModels.ListOperationArgs()
            {
                RequestTypes = types
            };
            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.ListRequestsAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(filterArgs));

            return Json(result.Select((request) => ConvertToRequestStatusModel(request)).OrderByDescending(m => m.SubmittedTime));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDemographicSubjectRequest([FromBody] PxsModels.DemographicRequest request)
        {
            var subject = ConvertToDemographicSubject(request.Subject);
            subject.Validate(SubjectUseContext.Delete);

            var deleteOperationArgs = new PxsApiModels.DeleteOperationArgs()
            {
                DataTypes = DemographicDeleteDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                Subject = subject,
                AccessToken = string.Empty,
                StartTime = DateTimeOffset.MinValue,
                EndTime = DateTimeOffset.MaxValue
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.PostDeleteRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(deleteOperationArgs));
            return Json(ConvertToOperationResponseModelForDelete(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMicrosoftEmployeeSubjectRequest([FromBody] PxsModels.MicrosoftEmployeeRequest request)
        {
            var subject = ConvertToMicrosoftEmployeeSubject(request.Subject);
            subject.Validate(SubjectUseContext.Delete);

            var deleteOperationArgs = new PxsApiModels.DeleteOperationArgs()
            {
                DataTypes = MicrosoftEmployeeDeleteDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                Subject = subject,
                AccessToken = string.Empty,
                StartTime = DateTimeOffset.MinValue,
                EndTime = request.Subject.EmploymentEndDate.HasValue ? request.Subject.EmploymentEndDate.Value : DateTimeOffset.MaxValue,
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = (await pxsClientProviderAccessor.ProviderInstance.Instance.PostDeleteRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(deleteOperationArgs)));
            return Json(ConvertToOperationResponseModelForDelete(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMsaSelfAuthSubjectRequest([FromBody] PxsModels.MsaSelfAuthRequest request)
        {
            var subject = new MsaSelfAuthSubject(request.Subject.ProxyTicket);
            subject.Validate(SubjectUseContext.Delete);

            var deleteOperationArgs = new PxsApiModels.DeleteOperationArgs()
            {
                DataTypes = MsaSelfAuthDeleteDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                Subject = subject,
                AccessToken = request.Subject.ProxyTicket,
                StartTime = DateTimeOffset.MinValue,
                EndTime = DateTimeOffset.MaxValue,
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.PostDeleteRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(deleteOperationArgs));
            return Json(ConvertToOperationResponseModelForDelete(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportDemographicSubjectRequest([FromBody] PxsModels.DemographicRequest request)
        {
            var subject = ConvertToDemographicSubject(request.Subject);
            subject.Validate(SubjectUseContext.Export);

            var exportOperationArgs = new PxsApiModels.ExportOperationArgs()
            {
                DataTypes = DemographicExportDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                Subject = subject,
                StartTime = DateTimeOffset.MinValue,
                EndTime = DateTimeOffset.MaxValue,
                StorageLocationUri = null
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.PostExportRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(exportOperationArgs));
            return Json(ConvertToOperationResponseModelForExport(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportMicrosoftEmployeeSubjectRequest([FromBody] PxsModels.MicrosoftEmployeeRequest request)
        {
            var subject = ConvertToMicrosoftEmployeeSubject(request.Subject);
            subject.Validate(SubjectUseContext.Export);

            var exportOperationArgs = new PxsApiModels.ExportOperationArgs()
            {
                DataTypes = MicrosoftEmployeeExportDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                Subject = subject,
                StartTime = DateTimeOffset.MinValue,
                EndTime = request.Subject.EmploymentEndDate.HasValue ? request.Subject.EmploymentEndDate.Value : DateTimeOffset.MaxValue,
                StorageLocationUri = null
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.PostExportRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(exportOperationArgs));
            return Json(ConvertToOperationResponseModelForExport(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportMsaSelfAuthSubjectRequest([FromBody] PxsModels.MsaSelfAuthRequest request)
        {
            var subject = new MsaSelfAuthSubject(request.Subject.ProxyTicket);
            subject.Validate(SubjectUseContext.Export);

            var exportOperationArgs = new PxsApiModels.ExportOperationArgs()
            {
                DataTypes = MsaSelfAuthExportDataTypes.ToList(),
                Context = CreateManualOperationContext(request.Metadata),
                AccessToken = request.Subject.ProxyTicket,
                Subject = subject,
                StartTime = DateTimeOffset.MinValue,
                EndTime = DateTimeOffset.MaxValue,
                StorageLocationUri = null
            };

            ApplyRequestMetadataToCorrelationContext(request.Metadata);

            var result = await pxsClientProviderAccessor.ProviderInstance.Instance.PostExportRequestAsync(
                await pxsClientProviderAccessor.ProviderInstance.ApplyRequestContext(exportOperationArgs));
            return Json(ConvertToOperationResponseModelForExport(result));
        }

        private PxsModels.OperationResponse ConvertToOperationResponseModelForDelete(
            PxsApiModels.DeleteOperationResponse operationResonse) => new PxsModels.OperationResponse()
        {
            Ids = operationResonse.Ids.Select(id => id.ToString())
        };

        private PxsModels.OperationResponse ConvertToOperationResponseModelForExport(
            PxsApiModels.ExportOperationResponse operationResonse) => new PxsModels.OperationResponse()
        {
            Ids = operationResonse.Ids.Select(id => id.ToString())
        };

        private DemographicSubject ConvertToDemographicSubject(PxsModels.DemographicRequest.DemographicSubject subject)
        {
            DemographicSubject.Address postalAddress;
            if ((subject.PostalAddress.StreetNumbers?.Any() ?? false) ||
                (subject.PostalAddress.StreetNames?.Any() ?? false) ||
                (subject.PostalAddress.UnitNumbers?.Any() ?? false) ||
                (subject.PostalAddress.Cities?.Any() ?? false) ||
                (subject.PostalAddress.Regions?.Any() ?? false) ||
                (subject.PostalAddress.PostalCodes?.Any() ?? false))
            {
                postalAddress = new DemographicSubject.Address()
                {
                    StreetNumbers = subject.PostalAddress.StreetNumbers?.ToList(),
                    StreetNames = subject.PostalAddress.StreetNames?.ToList(),
                    UnitNumbers = subject.PostalAddress.UnitNumbers?.ToList(),
                    Cities = subject.PostalAddress.Cities?.ToList(),
                    Regions = subject.PostalAddress.Regions?.ToList(),
                    PostalCodes = subject.PostalAddress.PostalCodes?.ToList()
                };
            }
            else
            {
                postalAddress = null;
            }

            return new DemographicSubject()
            {
                Names = subject.Names?.ToList(),
                Emails = subject.Emails?.ToList(),
                Phones = subject.PhoneNumbers?.ToList(),
                PostalAddress = postalAddress
            };
        }

        private MicrosoftEmployeeSubject ConvertToMicrosoftEmployeeSubject(
            PxsModels.MicrosoftEmployeeRequest.MicrosoftEmployeeSubject subject) => new MicrosoftEmployeeSubject()
        {
            Emails = subject.Emails?.ToList(),
            EmployeeId = subject.EmployeeId,
            EmploymentStart = subject.EmploymentStartDate,
            EmploymentEnd = subject.EmploymentEndDate
        };

        private PxsModels.PrivacyRequestStatus ConvertToRequestStatusModel(
            Pxs.Contracts.PrivacyRequestStatus requestStatus) => new PxsModels.PrivacyRequestStatus()
        {
            Context = requestStatus.Context,
            DestinationUri = requestStatus.DestinationUri,
            Id = requestStatus.Id,
            State = ConvertToPrivacyRequestStateModel(requestStatus.State),
            SubjectType = GetSubjectName(requestStatus.Subject),
            SubmittedTime = requestStatus.SubmittedTime,
            CompletedTime = (Pxs.Contracts.PrivacyRequestState.Completed == requestStatus.State) ? requestStatus.CompletedTime : (DateTimeOffset?)null,
            Progress = requestStatus.CompletionSuccessRate > 0 ? Convert.ToInt32(requestStatus.CompletionSuccessRate * 100.0) : (int?)null
        };

        private string GetSubjectName(CommandFeed.Contracts.Subjects.IPrivacySubject subject)
        {
            switch (subject)
            {
                case CommandFeed.Contracts.Subjects.DemographicSubject demographicSubject:
                    // PCF loses strict type, so we infer the intended type.
                    return "DemographicSubject";

                case CommandFeed.Contracts.Subjects.MsaSubject msaSubject:
                    return "MsaSubject";

                case CommandFeed.Contracts.Subjects.MicrosoftEmployee microsoftEmployee:
                    return "MicrosoftEmployee";

                default:
                    return "Unknown";
            }
        }

        private PxsModels.PrivacyRequestState ConvertToPrivacyRequestStateModel(Pxs.Contracts.PrivacyRequestState requestState)
        {
            switch (requestState)
            {
                case Pxs.Contracts.PrivacyRequestState.Completed:
                    return PxsModels.PrivacyRequestState.Completed;

                case Pxs.Contracts.PrivacyRequestState.Submitted:
                    return PxsModels.PrivacyRequestState.Submitted;

                default:
                    throw new NotSupportedException($"Unknown request state: {requestState}");
            }
        }

        private string CreateManualOperationContext(PxsModels.ManualRequestMetadata metadata)
        {
            metadata.ManualRequestSubmitter = User.Identity.Name;
            return Newtonsoft.Json.JsonConvert.SerializeObject(metadata);
        }

        /// <summary>
        /// Applies manual request metadata to correlation context to enable tracking of 
        /// cross-service privacy scenarios.
        /// </summary>
        private void ApplyRequestMetadataToCorrelationContext(PxsModels.ManualRequestMetadata requestMetadata)
        {
            instrumentedRequestContextAccessor.GetInstrumentedRequestContext().MonitoringContext
                .CorrelationContext.TrySet(CorrelationContextProperty.Market, requestMetadata.CountryOfResidence);
        }
    }
}
