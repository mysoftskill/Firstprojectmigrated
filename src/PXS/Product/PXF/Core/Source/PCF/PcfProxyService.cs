// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.PCF
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.Core.Throttling;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Newtonsoft.Json;
    using static Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.AadRequestVerificationServiceAdapter;

    internal enum ListActorAuthorization
    {
        /// <summary>
        ///     Unauthorized. Caller is not authorized to see export requests.
        /// </summary>
        Unauthorized,

        /// <summary>
        ///     ListActorOnly. Caller is authorized to only see their own export requests.
        /// </summary>
        ListActorOnly,

        /// <summary>
        ///     ListAll. Caller is authorized to see all export requests.
        /// </summary>
        ListAll
    }

    /// <inheritdoc />
    public class PcfProxyService : IPcfProxyService
    {
        public const string RequestInfoBlobName = "RequestInfo.json";

        private const string DeleteApiName = "Delete";

        private const string ExportApiName = "Export";

        private const string AccountCloseApiName = "AccountClose";

        private const string ListByCallerApiName = "ListByCaller";

        private const string ListByUserApiName = "ListByUser";

        private const string LookupByIdApiName = "LookupById";

        // Verifier Adapters have a limit of characters in a claim, and a guid is 36 characters (plus 1 for the delimiter), so max must be less than that
        internal const int MaxMsaCommandCountPerVerifier = MsaIdentityServiceAdapter.MaxValuePerClaim / (36 + 1);

        internal const int MaxAadCommandCountPerVerifier = AadRequestVerificationServiceAdapter.MaxValuePerClaim / (36 + 1);

        private const string RequestInfoDescription =
            "This file is included to ensure a tenant admin can associate the exported data with the right user and tenant and to validate " +
            "that Microsoft has write access to the Azure Storage prior to exporting data. You can ignore this file.";

        /// <summary>
        ///     This is the SG groups for PRC admins. This is provided by PRCAdmins@Microsoft.com.
        /// </summary>
        private readonly IAadRequestVerificationServiceAdapter aadRvsAdapter;

        private readonly Dictionary<string, IRequestThrottler> callerIdThrottles;

        private readonly ICounterFactory counterFactory;

        private readonly IGraphAdapter graphAdapter;

        private readonly ILogger logger;

        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        private readonly IPcfAdapter pcfAdapter;

        private readonly Policy policy;

        private readonly IPrivacyConfigurationManager privacyConfigurationManager;

        private readonly IVerificationTokenValidationService verificationTokenValidationService;

        private readonly IXboxAccountsAdapter xboxAccountsAdapter;

        private readonly IAppConfiguration appConfiguration;

        public static ServiceResponse<T> HandleMsaIdentityAdapterError<T>(AdapterError adapterError)
        {
            switch (adapterError.Code)
            {
                case AdapterErrorCode.MsaCallerNotAuthorized:
                case AdapterErrorCode.MsaUserNotAuthorized:
                    return new ServiceResponse<T> { Error = new Error(ErrorCode.Unauthorized, adapterError.Message) };
                case AdapterErrorCode.TimeWindowExpired:
                    return new ServiceResponse<T> { Error = new Error(ErrorCode.TimeWindowExpired, adapterError.Message) };
                default:

                    // Treat unknown as server errors
                    return new ServiceResponse<T> { Error = new Error(ErrorCode.Unknown, adapterError.Message) };
            }
        }

        public static PrivacyRequestStatus ToPrivacyRequestStatus(CommandStatusResponse response)
        {
            Enum.TryParse(response.CommandType, out PrivacyRequestType requestType);
            return new PrivacyRequestStatus(
                response.CommandId,
                requestType,
                response.CreatedTime,
                response.CompletedTime,
                response.Subject,
                response.DataTypes?.ToList() ?? new List<string>(),
                response.Context,
                response.IsGloballyComplete ? PrivacyRequestState.Completed : PrivacyRequestState.Submitted,
                response.FinalExportDestinationUri,
                response.CompletionSuccessRate,
                requestType != PrivacyRequestType.Export ? ExperienceContracts.ExportArchivesDeleteStatus.NotApplicable : (ExperienceContracts.ExportArchivesDeleteStatus)response.ExportArchivesDeleteStatus);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PcfProxyService" /> class.
        /// </summary>
        public PcfProxyService(
            IXboxAccountsAdapter xboxAccountsAdapter,
            IPcfAdapter pcfAdapter,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            IVerificationTokenValidationService verificationTokenValidationService,
            IAadRequestVerificationServiceAdapter aadRvsAdapter,
            IGraphAdapter graphAdapter,
            IPrivacyConfigurationManager privacyConfigurationManager,
            ILogger logger,
            ICounterFactory counterFactory,
            Policy policy,
            IAppConfiguration appConfiguration)
        {
            this.pcfAdapter = pcfAdapter ?? throw new ArgumentNullException(nameof(pcfAdapter));
            this.xboxAccountsAdapter = xboxAccountsAdapter ?? throw new ArgumentNullException(nameof(xboxAccountsAdapter));
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
            this.verificationTokenValidationService = verificationTokenValidationService ?? throw new ArgumentNullException(nameof(verificationTokenValidationService));
            this.aadRvsAdapter = aadRvsAdapter ?? throw new ArgumentNullException(nameof(aadRvsAdapter));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            this.graphAdapter = graphAdapter ?? throw new ArgumentNullException(nameof(graphAdapter));
            this.privacyConfigurationManager =
                privacyConfigurationManager ?? throw new ArgumentNullException(nameof(privacyConfigurationManager));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            this.callerIdThrottles = this.privacyConfigurationManager
                                         .PrivacyExperienceServiceConfiguration
                                         ?.ThrottleConfigurations
                                         ?.Values
                                         .ToDictionary(
                                             c => c.Id,
                                             c => (IRequestThrottler)new InMemoryRequestThrottler(
                                                 c.Id,
                                                 c.MaxRequestsPerPeriod,
                                                 TimeSpan.FromSeconds(c.PeriodInSeconds),
                                                 this.counterFactory)) ??
                                     new Dictionary<string, IRequestThrottler>();
        }

        public async Task<ServiceResponse<PrivacyRequestStatus>> ListMyRequestByIdAsync(IRequestContext requestContext, Guid commandId)
        {
            Error throttleError = this.ThrottleCaller(requestContext, LookupByIdApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<PrivacyRequestStatus>
                {
                    Error = throttleError
                };
            }

            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            if (portal == Portal.Pcd)
            {
                Error checkingPRCMembershipError = await this.CheckPrcMembershipAsync(requestContext).ConfigureAwait(false);
                if (checkingPRCMembershipError != null)
                {
                    return new ServiceResponse<PrivacyRequestStatus>
                    {
                        Error = checkingPRCMembershipError
                    };
                }

                return await this.ListRequestAsync(
                        requestContext.RequireIdentity<AadIdentity>().ApplicationId,
                        commandId,
                        this.pcfAdapter.GetRequestByIdAsync(commandId, false))
                    .ConfigureAwait(false);
            }

            if (requestContext.Identity is AadIdentity aadIdentity)
            {
                var aadRvsActorRequest = new AadRvsActorRequest
                {
                    TargetTenantId = aadIdentity.TenantId.ToString(),
                    TargetObjectId = aadIdentity.ObjectId.ToString(),
                    CorrelationId = LogicalWebOperationContext.ServerActivityId.ToString(),
                    CommandIds = commandId.ToString()
                };

                AdapterResponse<AadRvsScopeResponse> actorListAuthorization =
                    await this.aadRvsAdapter.ActorListAuthorizationAsync(aadRvsActorRequest, requestContext).ConfigureAwait(false);

                if (!actorListAuthorization.IsSuccess)
                {
                    return new ServiceResponse<PrivacyRequestStatus>
                    {
                        Error = new Error
                        {
                            Code = actorListAuthorization.Error.Code.ToServiceErrorCode().ToString(),
                            Message = $"ErrorCode: {actorListAuthorization.Error.Code}, ErrorMessage: {actorListAuthorization.Error.Message}"
                        }
                    };
                }

                ListActorAuthorization listActorAuthorization = ConvertAadRvsScopeResponse(actorListAuthorization.Result);

                // Only return their own requests for viral users.
                if (listActorAuthorization == ListActorAuthorization.ListActorOnly)
                {
                    if (aadIdentity.ObjectId == Guid.Empty)
                    {
                        return new ServiceResponse<PrivacyRequestStatus>
                        {
                            Error = new Error
                            {
                                Code = ErrorCode.Unknown.ToString(),
                                Message = "We are not able to retrieve the ObjectId of the caller."
                            }
                        };
                    }

                    ServiceResponse<PrivacyRequestStatus> result = await this.ListRequestAsync(
                        aadIdentity.TenantId.ToString(),
                        commandId,
                        this.pcfAdapter.GetRequestByIdAsync(commandId, false)).ConfigureAwait(false);

                    // ******************************* DANGER *******************************
                    // This if check is *extremely* important! It is what stops viral users from looking up the results for *other* viral users.
                    // This *must* not be broken, or we will have very bad data leak issue!!!
                    // ******************************* DANGER *******************************
                    if (result.Result != null && (!(result.Result.Subject is AadSubject aadSubject) || aadSubject.ObjectId != aadIdentity.ObjectId))
                    {
                        this.logger.Warning(nameof(PcfProxyService), $"Viral caller looking up commandId ({commandId}) they do not own so returning not found");
                        return new ServiceResponse<PrivacyRequestStatus>
                        {
                            Result = null
                        };
                    }

                    // ******************************* DANGER *******************************

                    return result;
                }

                // Return everything for a tenant admin in a regular tenant.
                if (listActorAuthorization == ListActorAuthorization.ListAll)
                {
                    return await this.ListRequestAsync(
                            aadIdentity.TenantId.ToString(),
                            commandId,
                            this.pcfAdapter.GetRequestByIdAsync(commandId, false))
                        .ConfigureAwait(false);
                }

                string aadRvsResponse = JsonConvert.SerializeObject(actorListAuthorization.Result);
                ErrorEvent errorEvent = new ErrorEvent
                {
                    ErrorCode = ErrorCode.Unauthorized.ToString(),
                    ErrorName = actorListAuthorization.Result?.Outcome,
                    ErrorDetails = $"AAD RVS Response: '{aadRvsResponse}'",
                    ExtraData = { ["tid"] = aadIdentity.TenantId.ToString() }
                };

                errorEvent.LogError(e =>
                {
                    var userInfo = new UserInfo();
                    userInfo.SetId(UserIdType.AzureAdId, aadIdentity.ObjectId.ToString());
                    userInfo.FillEnvelope(e);
                });

                return new ServiceResponse<PrivacyRequestStatus>
                {
                    Error = new Error
                    {
                        Code = ErrorCode.Unauthorized.ToString(),
                        Message = "User is not authorized for this operation"
                    }
                };
            }

            throw new InvalidOperationException($"Unknown {nameof(this.ListMyRequestByIdAsync)} scenario.");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<CommandStatusResponse>> ListRequestByIdAsync(IRequestContext requestContext, Guid commandId)
        {
            Error throttleError = this.ThrottleCaller(requestContext, LookupByIdApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<CommandStatusResponse>
                {
                    Error = throttleError
                };
            }

            Error checkingMembershipError = await this.CheckGetRequestByIdMembershipAsync(requestContext).ConfigureAwait(false);
            if (checkingMembershipError != null)
            {
                return new ServiceResponse<CommandStatusResponse>
                {
                    Error = checkingMembershipError
                };
            }

            // We need to redact sensitive information. Keep this code in one place, by calling PCF to do this work rather than keeping in sync
            AdapterResponse<CommandStatusResponse> pcfResponse = await this.pcfAdapter.GetRequestByIdAsync(commandId, redacted: true).ConfigureAwait(false);

            // If it fails, return failure
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<CommandStatusResponse> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            return new ServiceResponse<CommandStatusResponse> { Result = pcfResponse.Result };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IList<PrivacyRequestStatus>>> ListRequestsByCallerMsaAsync(IRequestContext requestContext, params RequestType[] requestTypes)
        {
            Error throttleError = this.ThrottleCaller(requestContext, ListByUserApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<IList<PrivacyRequestStatus>>
                {
                    Error = throttleError
                };
            }

            ServiceResponse<IList<PrivacyRequestStatus>> list = await this.ListRequestsAsync(
                this.pcfAdapter.QueryCommandStatusAsync(
                 new MsaSubject { Puid = requestContext.TargetPuid },
                 null,
                 requestTypes,
                 DateTimeOffset.MinValue)).
            ConfigureAwait(false);
            return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = list.Result.Where(i => i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteInProgress
            && i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteCompleted).ToList() };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IList<PrivacyRequestStatus>>> ListRequestsByCallerSiteAsync(IRequestContext requestContext, params RequestType[] requestTypes)
        {
            ServiceResponse<IList<PrivacyRequestStatus>> list;
            Error throttleError = this.ThrottleCaller(requestContext, ListByCallerApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<IList<PrivacyRequestStatus>>
                {
                    Error = throttleError
                };
            }

            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            if (portal == Portal.Pcd)
            {
                Error checkingPRCMembershipError = await this.CheckPrcMembershipAsync(requestContext).ConfigureAwait(false);
                if (checkingPRCMembershipError != null)
                {
                    return new ServiceResponse<IList<PrivacyRequestStatus>>
                    {
                        Error = checkingPRCMembershipError
                    };
                }

                list = await this.ListRequestsAsync(
                            this.pcfAdapter.QueryCommandStatusAsync(null, requestContext.RequireIdentity<AadIdentity>().ApplicationId, requestTypes, DateTimeOffset.MinValue))
                .ConfigureAwait(false);
                return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = list.Result.Where(i => i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteInProgress
                && i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteCompleted).ToList() };
            }

            if (requestContext.Identity is AadIdentity aadIdentity)
            {
                var aadRvsActorRequest = new AadRvsActorRequest
                {
                    TargetTenantId = aadIdentity.TenantId.ToString(),
                    TargetObjectId = aadIdentity.ObjectId.ToString(),
                    CorrelationId = LogicalWebOperationContext.ServerActivityId.ToString()
                };

                AdapterResponse<AadRvsScopeResponse> actorListAuthorization =
                    await this.aadRvsAdapter.ActorListAuthorizationAsync(aadRvsActorRequest, requestContext).ConfigureAwait(false);

                if (!actorListAuthorization.IsSuccess)
                {
                    return new ServiceResponse<IList<PrivacyRequestStatus>>
                    {
                        Error = new Error(actorListAuthorization.Error.Code.ToServiceErrorCode(), actorListAuthorization.Error.Message)
                    };
                }

                ListActorAuthorization listActorAuthorization = ConvertAadRvsScopeResponse(actorListAuthorization.Result);

                // Only return their own requests for viral users.
                if (listActorAuthorization == ListActorAuthorization.ListActorOnly)
                {
                    if (aadIdentity.ObjectId == Guid.Empty)
                    {
                        return new ServiceResponse<IList<PrivacyRequestStatus>>
                        {
                            Error = new Error
                            {
                                Code = ErrorCode.Unknown.ToString(),
                                Message = "We are not able to retrieve the ObjectId of the caller."
                            }
                        };
                    }

                    list = await this.ListRequestsAsync(
                            this.pcfAdapter.QueryCommandStatusAsync(
                            new AadSubject { ObjectId = aadIdentity.ObjectId },
                            aadIdentity.TenantId.ToString(),
                            requestTypes,
                            DateTimeOffset.MinValue)).ConfigureAwait(false);
                    return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = list.Result.Where(i => i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteInProgress
                    && i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteCompleted).ToList() };
                }

                // Return everything for a tenant admin in a regular tenant.
                if (listActorAuthorization == ListActorAuthorization.ListAll)
                {
                    list = await this.ListRequestsAsync(this.pcfAdapter.QueryCommandStatusAsync(null, aadIdentity.TenantId.ToString(), requestTypes, DateTimeOffset.MinValue)).ConfigureAwait(false);
                    return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = list.Result.Where(i => i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteInProgress
                    && i.ExportArchivesDeleteStatus != ExperienceContracts.ExportArchivesDeleteStatus.DeleteCompleted).ToList() };
                }

                return new ServiceResponse<IList<PrivacyRequestStatus>>
                {
                    Error = new Error
                    {
                        Code = ErrorCode.Unauthorized.ToString(),
                        Message = "User is not authorized for this operation"
                    }
                };
            }

            throw new InvalidOperationException($"Unknown {nameof(this.ListRequestsByCallerSiteAsync)} scenario.");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IList<Guid>>> PostDeleteRequestsAsync(
            IRequestContext requestContext,
            List<DeleteRequest> requests)
        {
            if (requests == null || requests.Count() == 0)
            {
                return new ServiceResponse<IList<Guid>>
                {
                    Error = new Error(ErrorCode.InvalidInput, "Delete request list is null or empty")
                };
            }

            Error throttleError = this.ThrottleCaller(requestContext, DeleteApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<IList<Guid>>
                {
                    Error = throttleError
                };
            }

            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            if (portal == Portal.Pcd)
            {
                Error checkingPRCMembershipError = await this.CheckPrcMembershipAsync(requestContext).ConfigureAwait(false);
                if (checkingPRCMembershipError != null)
                {
                    return new ServiceResponse<IList<Guid>>
                    {
                        Error = checkingPRCMembershipError
                    };
                }
            }

            // Get any MSA subjects, and validate they are for the current user and not arbitrary others
            // For any AAD subjects, AADRVS owns the authorization of who can perform actions.
            List<DeleteRequest> msaSubjectRequests = requests.Where(s => s.Subject is MsaSubject).ToList();
            if (msaSubjectRequests.Any())
            {
                IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();
                if (msaSubjectRequests.Select(r => r.Subject).Cast<MsaSubject>().Any(s => s.Puid != pxfRequestContext.TargetPuid))
                {
                    return new ServiceResponse<IList<Guid>>
                    {
                        Error = new Error(ErrorCode.Unauthorized, "Cannot submit MSA subject different than the current user")
                    };
                }

                Task<AdapterResponse<Dictionary<long, string>>> xuidResponseTask =
                    this.xboxAccountsAdapter.GetXuidsAsync(new List<long> { pxfRequestContext.TargetPuid });

                await xuidResponseTask.ConfigureAwait(false);

                AdapterResponse<Dictionary<long, string>> xuidResponse = xuidResponseTask.Result;
                if (!xuidResponse.IsSuccess)
                {
                    return new ServiceResponse<IList<Guid>> { Error = new Error(ErrorCode.PartnerError, xuidResponse.Error.ToString()) };
                }

                if (!xuidResponse.Result.TryGetValue(pxfRequestContext.TargetPuid, out string xuid))
                {
                    return new ServiceResponse<IList<Guid>> { Error = new Error(ErrorCode.PartnerError, $"Cannot retrieve XUID for PUID {pxfRequestContext.TargetPuid}") };
                }

                // Complete the MSA subjects with a xuid
                foreach (MsaSubject msaSubject in msaSubjectRequests.Select(r => r.Subject).Cast<MsaSubject>())
                {
                    msaSubject.Xuid = xuid;
                }
            }

            ServiceResponse<IList<Guid>> addVerifierResponse = await this.AddVerifiersToSupportedDeleteRequestsAsync(requests, requestContext).ConfigureAwait(false);

            if (!addVerifierResponse.IsSuccess)
            {
                return addVerifierResponse;
            }

            // Write PCF requests to PCF
            AdapterResponse pcfResponse = await this.pcfAdapter.PostCommandsAsync(
                requests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);

            // If write fails, fail the whole request
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<IList<Guid>>
                {
                    Error = new Error
                    {
                        Code = pcfResponse.Error.StatusCode.ToString(),
                        ErrorDetails = pcfResponse.Error.Message
                    }
                };
            }

            return new ServiceResponse<IList<Guid>> { Result = requests.Select(r => r.RequestId).ToList() };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<Guid>> PostExportRequestAsync(IRequestContext requestContext, ExportRequest request)
        {
            Error throttleError = this.ThrottleCaller(requestContext, ExportApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<Guid>
                {
                    Error = throttleError
                };
            }

            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            if (portal == Portal.Pcd)
            {
                Error checkingPRCMembershipError = await this.CheckPrcMembershipAsync(requestContext).ConfigureAwait(false);
                if (checkingPRCMembershipError != null)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = checkingPRCMembershipError
                    };
                }
            }

            bool shouldValidateExternalStorageAccess = false;

            if (request.StorageUri == null)
            {
                Type[] subjectsAllowedToUsePcfManagedStorage = { typeof(MsaSubject), typeof(DemographicSubject), typeof(MicrosoftEmployee) };

                if (!subjectsAllowedToUsePcfManagedStorage.Any(allowedSubjectType => allowedSubjectType.IsInstanceOfType(request.Subject)))
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.InvalidInput, $"Subject type {request.Subject.GetType().Name} is not allowed to use storage managed by PCF.")
                    };
                }

                AdapterResponse<IList<Uri>> pcfResponse = await this.pcfAdapter.GetPcfStorageUrisAsync().ConfigureAwait(false);
                if (!pcfResponse.IsSuccess)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error
                        {
                            Code = pcfResponse.Error.Code.ToString(),
                            Message = pcfResponse.Error.ToString()
                        }
                    };
                }

                if (pcfResponse.Result.Count < 1)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error
                        {
                            Code = "BadConfig",
                            ErrorDetails = "Bad Configuration: No storage configured"
                        }
                    };
                }

                request.StorageUri = pcfResponse.Result[Math.Abs((int)DateTime.UtcNow.Ticks % pcfResponse.Result.Count)];

                shouldValidateExternalStorageAccess = false; // Internal to pcf storage is used, no need to validate
            }
            else
            {
                // we are getting storage location from client, verify that we can write to it. Do after Authz
                shouldValidateExternalStorageAccess = true;
            }

            switch (request.Subject)
            {
                case AadSubject aadSubject:
                    var aadIdentity = requestContext.RequireIdentity<AadIdentity>();
                    if (aadSubject.ObjectId != aadIdentity.TargetObjectId)
                    {
                        return new ServiceResponse<Guid> { Error = new Error(ErrorCode.InvalidInput, "AadSubject and request context do not match on TargetObjectId") };
                    }

                    if (aadSubject.TenantId != aadIdentity.TenantId)
                    {
                        return new ServiceResponse<Guid> { Error = new Error(ErrorCode.InvalidInput, "AadSubject and request context do not match on TenantId") };
                    }

                    AdapterResponse aadVerifierResponse = await this.GetValidateAndAddVerifierTokenExportAsync(
                        request,
                        requestContext).ConfigureAwait(false);
                    if (!aadVerifierResponse.IsSuccess)
                    {
                        this.logger.Error(nameof(PcfProxyService), $"PostExportRequestAsync: GetValidateAndAddVerifierTokenExportAsync Failed: {aadVerifierResponse.Error}.");
                        return HandleAadRvsAdapterError<Guid>(aadVerifierResponse.Error);
                    }

                    break;

                case MsaSubject msaSubject:
                    if (msaSubject.Puid != requestContext.TargetPuid)
                    {
                        return new ServiceResponse<Guid>
                        {
                            Error = new Error(ErrorCode.Unauthorized, "Cannot submit MSA subject different than the current user")
                        };
                    }

                    AdapterResponse<Dictionary<long, string>> xuidResponse =
                        await this.xboxAccountsAdapter.GetXuidsAsync(new List<long> { requestContext.TargetPuid }).ConfigureAwait(false);
                    if (!xuidResponse.IsSuccess)
                    {
                        return new ServiceResponse<Guid> { Error = new Error(ErrorCode.PartnerError, xuidResponse.Error.ToString()) };
                    }

                    if (!xuidResponse.Result.TryGetValue(requestContext.TargetPuid, out string xuid))
                    {
                        return new ServiceResponse<Guid> { Error = new Error(ErrorCode.PartnerError, $"Cannot retrieve XUID for PUID {requestContext.TargetPuid}") };
                    }

                    // Complete the MSA subject with a xuid
                    msaSubject.Xuid = xuid;

                    // Get verifier tokens from MSA, and include them in the PCF requests, only for MSA and Device Subjects.
                    AdapterResponse msaVerifierResponse = await this.GetValidateAndAddVerifierTokenExportAsync(
                        request,
                        requestContext).ConfigureAwait(false);
                    if (!msaVerifierResponse.IsSuccess)
                    {
                        return HandleMsaIdentityAdapterError<Guid>(msaVerifierResponse.Error);
                    }

                    break;

                case DemographicSubject _:
                case MicrosoftEmployee _:

                    //  We're not doing anything special for demographic/microsoftemployee subjects.
                    break;

                default:
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.Unknown, $"Export does not support subject of type {request.Subject.GetType().Name}.")
                    };
            }

            if(shouldValidateExternalStorageAccess)
            {
                ServiceResponse<Guid> storageError = await ValidateExternalStorageAsync(request, this.logger).ConfigureAwait(false);
                if (storageError != null)
                {
                    return storageError;
                }
            }

            AdapterResponse pcfTask = await this.pcfAdapter.PostCommandsAsync(new PrivacyRequest[] { request }).ConfigureAwait(false);
            if (!pcfTask.IsSuccess)
            {
                this.logger.Error(nameof(PcfProxyService), $"PostExportRequestAsync: PostCommands Failed: {pcfTask.Error}.");

                return new ServiceResponse<Guid>
                {
                    Error = new Error
                    {
                        Code = pcfTask.Error.Code.ToString(),
                        Message = pcfTask.Error.Message
                    }
                };
            }

            return new ServiceResponse<Guid>
            {
                Result = request.RequestId
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<Guid>> PostAccountCleanupRequestAsync(IRequestContext requestContext, AccountCloseRequest request)
        {
            Error throttleError = this.ThrottleCaller(requestContext, AccountCloseApiName);
            if (throttleError != null)
            {
                return new ServiceResponse<Guid>
                {
                    Error = throttleError
                };
            }

            // TODO: Do we want to be able to call this endpoint directly?
            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            if (portal != Portal.MsGraph && portal != Portal.PxsAadTest)
            {
                return new ServiceResponse<Guid>
                {
                    Error = new Error(ErrorCode.InvalidInput, $"AccountCleanup is not supported from {portal}.")
                };

            }

            switch (request.Subject)
            {
                case AadSubject aadSubject:
                    var aadIdentity = requestContext.RequireIdentity<AadIdentity>();
                    if (aadSubject.ObjectId != aadIdentity.TargetObjectId)
                    {
                        return new ServiceResponse<Guid> { Error = new Error(ErrorCode.InvalidInput, "AadSubject and request context do not match on TargetObjectId") };
                    }

                    AdapterResponse aadVerifierResponse = await this.GetValidateAndAddVerifierTokenAccountCleanupAsync(
                        request,
                        requestContext).ConfigureAwait(false);
                    if (!aadVerifierResponse.IsSuccess)
                    {
                        this.logger.Error(nameof(PcfProxyService), $"PostAccountCleanup: GetValidateAndAddVerifierTokenAccountCleanupAsync Failed: {aadVerifierResponse.Error}.");
                        return HandleAadRvsAdapterError<Guid>(aadVerifierResponse.Error);
                    }

                    break;

                default:
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.Unknown, $"AccountCleanup does not support subject of type {request.Subject.GetType().Name}.")
                    };
            }

            AdapterResponse pcfTask = await this.pcfAdapter.PostCommandsAsync(new PrivacyRequest[] { request }).ConfigureAwait(false);
            if (!pcfTask.IsSuccess)
            {
                this.logger.Error(nameof(PcfProxyService), $"PostAccountCleanup: PostCommandsAsync: {pcfTask.Error}.");

                return new ServiceResponse<Guid>
                {
                    Error = new Error
                    {
                        Code = pcfTask.Error.Code.ToString(),
                        Message = pcfTask.Error.Message
                    }
                };
            }

            return new ServiceResponse<Guid>
            {
                Result = request.RequestId
            };
        }

        public async Task<ServiceResponse<IList<AssetGroupQueueStatistics>>> TestAgentQueueStatsAsync(IRequestContext requestContext, Guid agentId)
        {
            // First, get the un-redacted response.
            AdapterResponse<AgentQueueStatisticsResponse> pcfResponse = await this.pcfAdapter.GetAgentQueueStatsAsync(agentId).ConfigureAwait(false);

            // If it fails, return failure
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<IList<AssetGroupQueueStatistics>> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            return new ServiceResponse<IList<AssetGroupQueueStatistics>> { Result = pcfResponse.Result.AssetGroupQueueStatistics ?? new List<AssetGroupQueueStatistics>() };
        }

        public async Task<ServiceResponse> TestForceCommandCompletionAsync(IRequestContext requestContext, Guid commandId)
        {
            Error checkForceAllowed = await CheckCanForceCompleteAsync().ConfigureAwait(false);
            if (checkForceAllowed == null)
            {
                AdapterResponse pcfResponse = await this.pcfAdapter.ForceCompleteAsync(commandId).ConfigureAwait(false);

                // If it fails, return failure
                return !pcfResponse.IsSuccess
                    ? new ServiceResponse { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) }
                    : new ServiceResponse();
            }

            return new ServiceResponse { Error = checkForceAllowed };

            async Task<Error> CheckCanForceCompleteAsync()
            {
                Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
                if (requestContext.Identity is MsaSelfIdentity)
                {
                    return await this.CheckMsaRequesterOwnsCommandAsync(requestContext, commandId).ConfigureAwait(false);
                }

                if (portal == Portal.Pcd)
                {
                    return await this.CheckPrcMembershipAsync(requestContext).ConfigureAwait(false);
                }

                return new Error(ErrorCode.Unauthorized, $"Force complete not allowed for {requestContext.Identity} through {portal} portal");
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<CommandStatusResponse>> TestRequestByIdAsync(IRequestContext requestContext, Guid commandId)
        {
            // First, get the un-redacted response.
            AdapterResponse<CommandStatusResponse> pcfResponse = await this.pcfAdapter.GetRequestByIdAsync(commandId, false).ConfigureAwait(false);

            // If it fails, return failure
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<CommandStatusResponse> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            // If there is no result, return no result.
            if (pcfResponse.Result == null)
            {
                return new ServiceResponse<CommandStatusResponse> { Result = null };
            }

            // If the subject is MSA and the subject matches who's asking, just return the result
            bool isMyRequest = pcfResponse.Result.Subject is MsaSubject msaSubject && msaSubject.Puid == requestContext.TargetPuid;
            if (isMyRequest)
            {
                return new ServiceResponse<CommandStatusResponse> { Result = pcfResponse.Result };
            }

            // Otherwise, we need to redact sensitive information. Keep this code in one place, by calling PCF to do this work rather than keeping in sync
            pcfResponse = await this.pcfAdapter.GetRequestByIdAsync(commandId, true).ConfigureAwait(false);

            // If it fails, return failure
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<CommandStatusResponse> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            return new ServiceResponse<CommandStatusResponse> { Result = pcfResponse.Result };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IList<CommandStatusResponse>>> TestRequestByUserAsync(IRequestContext requestContext)
        {
            // First, get all the commands this site has created from PCF
            AdapterResponse<IList<CommandStatusResponse>> pcfResponse = await this.pcfAdapter.QueryCommandStatusAsync(
                new MsaSubject { Puid = requestContext.TargetPuid },
                null,
                null,
                DateTimeOffset.MinValue).ConfigureAwait(false);

            // If it fails, return failure
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<IList<CommandStatusResponse>> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            return new ServiceResponse<IList<CommandStatusResponse>> { Result = pcfResponse.Result };
        }

        private async Task<ServiceResponse<IList<Guid>>> AddVerifiersToSupportedDeleteRequestsAsync(List<DeleteRequest> requests, IRequestContext requestContext)
        {
            // Get verifier tokens from MSA or AAD RVS, and include them in the PCF requests
            // But only for subjects that support verifiers for DeleteRequest: MsaSubject, DeviceSubject and AadSubject Subjects
            bool SupportedDeleteMsaVerifierSubjectPredicate(DeleteRequest r) => r.Subject is MsaSubject || r.Subject is DeviceSubject;
            bool SupportedDeleteAadVerifierSubjectPredicate(DeleteRequest r) => r.Subject is AadSubject;

            if (requests.Where(SupportedDeleteMsaVerifierSubjectPredicate).Count() != 0)
            {
                AdapterResponse<IList<DeleteRequest>> msaIdentityAdapterResponse = await this.GetValidateAndAddVerifierTokenDeletesAsync(
                    requests.Where(SupportedDeleteMsaVerifierSubjectPredicate).ToList(),
                    requestContext).ConfigureAwait(false);

                if (!msaIdentityAdapterResponse.IsSuccess)
                {
                    return HandleMsaIdentityAdapterError<IList<Guid>>(msaIdentityAdapterResponse.Error);
                }

                requests.RemoveAll(SupportedDeleteMsaVerifierSubjectPredicate);
                requests.AddRange(msaIdentityAdapterResponse.Result);
            }

            if (requests.Where(SupportedDeleteAadVerifierSubjectPredicate).Count() != 0)
            {
                AdapterResponse<IList<DeleteRequest>> aadAdapterResponse = await this.GetValidateAndAddVerifierTokenDeletesAsync(
                    requests.Where(SupportedDeleteAadVerifierSubjectPredicate).ToList(),
                    requestContext).ConfigureAwait(false);

                if (!aadAdapterResponse.IsSuccess)
                {
                    return HandleAadRvsAdapterError<IList<Guid>>(aadAdapterResponse.Error);
                }

                requests.RemoveAll(SupportedDeleteAadVerifierSubjectPredicate);
                requests.AddRange(aadAdapterResponse.Result);
            }

            return new ServiceResponse<IList<Guid>> { Result = requests.Select(c => c.RequestId).ToList() };
        }

        private async Task<Error> CheckGetRequestByIdMembershipAsync(IRequestContext requestContext)
        {
            Error result = null;
            foreach (string sgId in this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.GetRequestByIdSecurityGroups)
            {
                if (!Guid.TryParse(sgId, out Guid securityGroupId))
                {
                    return new Error(ErrorCode.Unknown, "Cannot parse security group ID");
                }

                result = await this.CheckSecurityGroupMembershipAsync(requestContext, securityGroupId).ConfigureAwait(false);

                if (result == null)
                {
                    return null;
                }
            }

            return result;
        }

        private async Task<Error> CheckMsaRequesterOwnsCommandAsync(IRequestContext requestContext, Guid commandId)
        {
            // Get the current command status
            AdapterResponse<CommandStatusResponse> commandStatus = await this.pcfAdapter.GetRequestByIdAsync(commandId, false).ConfigureAwait(false);
            if (!commandStatus.IsSuccess)
            {
                return new Error(ErrorCode.PartnerError, commandStatus.Error.ToString());
            }

            if (commandStatus.Result?.Subject is MsaSubject subject && subject.Puid == requestContext.TargetPuid)
            {
                // Same user issued this command, no error
                return null;
            }

            return new Error(ErrorCode.Unauthorized, "User is not authorized to view/edit this command");
        }

        private async Task<Error> CheckPrcMembershipAsync(IRequestContext requestContext)
        {
            if (!Guid.TryParse(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.PRCSecurityGroup, out Guid prcSecurityGroupId))
            {
                return new Error(ErrorCode.Unknown, "Cannot parse security group ID");
            }

            return await this.CheckSecurityGroupMembershipAsync(requestContext, prcSecurityGroupId).ConfigureAwait(false);
        }

        private async Task<Error> CheckSecurityGroupMembershipAsync(IRequestContext requestContext, Guid securityGroupId)
        {
            if (requestContext.Identity is AadIdentity aadIdentity)
            {
                AdapterResponse<IsMemberOfResponse> isMemberOfResponse =
                    await this.graphAdapter.IsMemberOfAsync(
                        aadIdentity.ObjectId,
                        securityGroupId).ConfigureAwait(false);
                if (!isMemberOfResponse.IsSuccess)
                {
                    return new Error(ErrorCode.PartnerError, $"Cannot call AAD Graph to verify SG membership for user {aadIdentity.ObjectId}.");
                }

                if (!isMemberOfResponse.Result.Value)
                {
                    return new Error(ErrorCode.Unauthorized, $"User {aadIdentity.ObjectId} does not have sufficient rights to submit this request.");
                }

                return null;
            }

            return new Error(ErrorCode.Unauthorized, "The service only supports security group with AAD authentication for this API.");
        }

        private async Task<AdapterResponse<IList<DeleteRequest>>> GetValidateAndAddVerifierTokenDeletesAsync(
            IList<DeleteRequest> pcfDeleteRequests,
            IRequestContext requestContext)
        {
            IList<DeleteRequestAndVerifierResponse> deleteRequestAndVerifierResponseList = new List<DeleteRequestAndVerifierResponse>();

            // Build a dictionary keyed off the Subject. Requests are made to RVS by the subject for batching/efficiency.
            // But there's no guarantee that actual Subject objects are equal, so create the dictionary differently for each Subject (Msa, Device, Aad, etc.)
            Dictionary<IPrivacySubject, List<DeleteRequest>> subjectDictionary = CreateSubjectRequestDictionary(pcfDeleteRequests);

            foreach (KeyValuePair<IPrivacySubject, List<DeleteRequest>> subjectRequest in subjectDictionary)
            {
                switch (subjectRequest.Key)
                {
                    case MsaSubject msaSubject:
                        // Determine if these requests are a Bing Scoped Delete for a SearchRequestsAndQuery data type.
                        string dataType = null;
                        Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
                        if (portal == Portal.Bing)
                        {
                            string searchRequestsAndQueryDataTypeValue = Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value;
                            if (subjectRequest.Value.All(x => x.PrivacyDataType == searchRequestsAndQueryDataTypeValue))
                            {
                                // All requests must be of the same data type so that it can be included in the Scoped Delete verifier.
                                dataType = searchRequestsAndQueryDataTypeValue;
                            }
                            // If the requests are not all of the same data type, then they should follow the regular Delete flow with not data type.
                        }

                        // Verifier claims have a limit of characters in a claim, so append to a list until the max count is reached.
                        IList<DeleteRequest> pendingDeleteRequests = new List<DeleteRequest>();

                        foreach (DeleteRequest deleteRequest in subjectRequest.Value)
                        {
                            pendingDeleteRequests.Add(deleteRequest);

                            if (pendingDeleteRequests.Count >= MaxMsaCommandCountPerVerifier)
                            {
                                // Enough are collected to make the verifier request.
                                // Await the task too since multiple requests to MSA for the same subject can result in a DB error on MSA's side.
                                Task<AdapterResponse<string>> msaTask = this.msaIdentityServiceAdapter.GetGdprUserDeleteVerifierAsync(
                                    pendingDeleteRequests.Select(c => c.RequestId).ToList(),
                                    requestContext.ToAdapterRequestContext(),
                                    msaSubject.Xuid,
                                    predicate: null,
                                    dataType: dataType);
                                await msaTask.ConfigureAwait(false);
                                deleteRequestAndVerifierResponseList.Add(new DeleteRequestAndVerifierResponse(pendingDeleteRequests, msaTask, nameof(this.msaIdentityServiceAdapter)));

                                // Note: not clearing the list because it would clear the reference included in the response object.
                                pendingDeleteRequests = new List<DeleteRequest>();
                            }
                        }

                        // After iterating through all of them for the subject, if there's anything left (likely), make the verifier request.
                        if (pendingDeleteRequests.Count > 0)
                        {
                            deleteRequestAndVerifierResponseList.Add(
                                new DeleteRequestAndVerifierResponse(
                                    pendingDeleteRequests,
                                    this.msaIdentityServiceAdapter.GetGdprUserDeleteVerifierAsync(
                                        pendingDeleteRequests.Select(c => c.RequestId).ToList(),
                                        requestContext.ToAdapterRequestContext(),
                                        msaSubject.Xuid,
                                        predicate: null,
                                        dataType: dataType),
                            nameof(this.msaIdentityServiceAdapter)));
                        }

                        break;

                    case DeviceSubject deviceSubject:

                        // TODO: Can we do batching for device deletes too? ie Vortex device deletes are issued for multiple data types, but for the same device due to device expansion.
                        foreach (DeleteRequest deleteRequest in subjectRequest.Value)
                        {
                            deleteRequestAndVerifierResponseList.Add(
                                new DeleteRequestAndVerifierResponse(
                                    new List<DeleteRequest> { deleteRequest },
                                    this.msaIdentityServiceAdapter.GetGdprDeviceDeleteVerifierAsync(deleteRequest.RequestId, deviceSubject.GlobalDeviceId, predicate: null),
                                    nameof(this.msaIdentityServiceAdapter)));
                        }

                        break;

                    case AadSubject aadSubject:

                        // Validate all requests have the same processor and controller flags. If not, return error.
                        bool isControllerFlagValid = subjectRequest.Value.Select(r => r.ControllerApplicable).Distinct().Count() == 1;
                        bool isProcessorFlagValid = subjectRequest.Value.Select(r => r.ProcessorApplicable).Distinct().Count() == 1;

                        if (!isControllerFlagValid)
                        {
                            return new AdapterResponse<IList<DeleteRequest>> { Error = new AdapterError(AdapterErrorCode.BadRequest, "ControllerApplicableFlagNotConsistent", 500) };
                        }

                        if (!isProcessorFlagValid)
                        {
                            return new AdapterResponse<IList<DeleteRequest>> { Error = new AdapterError(AdapterErrorCode.BadRequest, "ProcessorApplicableFlagNotConsistent", 500) };
                        }

                        // Verifier claims have a limit of characters in a claim, so append to a list until the max count is reached.
                        IList<DeleteRequest> pendingAadDeleteRequests = new List<DeleteRequest>();

                        foreach (DeleteRequest deleteRequest in subjectRequest.Value)
                        {
                            pendingAadDeleteRequests.Add(deleteRequest);

                            if (pendingAadDeleteRequests.Count >= MaxAadCommandCountPerVerifier)
                            {
                                // Enough are collected to make the verifier request.
                                Task<AdapterResponse<string>> aadTask = this.aadRvsAdapter.ConstructDeleteAsync(
                                    new AadRvsRequest
                                    {
                                        TenantId = aadSubject.TenantId.ToString(),
                                        ObjectId = aadSubject.ObjectId.ToString(),
                                        ControllerApplicable = pendingAadDeleteRequests.First().ControllerApplicable,
                                        ProcessorApplicable = pendingAadDeleteRequests.First().ProcessorApplicable,
                                        CorrelationId = LogicalWebOperationContext.ServerActivityId.ToString(),
                                        CommandIds = string.Join(",", pendingAadDeleteRequests.Select(c => c.RequestId))
                                    },
                                    requestContext);

                                deleteRequestAndVerifierResponseList.Add(new DeleteRequestAndVerifierResponse(pendingAadDeleteRequests, aadTask, nameof(this.aadRvsAdapter)));

                                pendingAadDeleteRequests = new List<DeleteRequest>();
                            }
                        }

                        // After iterating through all of them for the subject, if there's anything left (likely), make the verifier request.
                        if (pendingAadDeleteRequests.Count > 0)
                        {
                            deleteRequestAndVerifierResponseList.Add(
                                new DeleteRequestAndVerifierResponse(
                                    pendingAadDeleteRequests,
                                    this.aadRvsAdapter.ConstructDeleteAsync(
                                        new AadRvsRequest
                                        {
                                            TenantId = aadSubject.TenantId.ToString(),
                                            ObjectId = aadSubject.ObjectId.ToString(),
                                            ControllerApplicable = pendingAadDeleteRequests.First().ControllerApplicable,
                                            ProcessorApplicable = pendingAadDeleteRequests.First().ProcessorApplicable,
                                            CorrelationId = LogicalWebOperationContext.ServerActivityId.ToString(),
                                            CommandIds = string.Join(",", pendingAadDeleteRequests.Select(c => c.RequestId))
                                        },
                                        requestContext),
                                   nameof(this.aadRvsAdapter)));
                        }

                        break;

                    default:
                        throw new NotSupportedException($"Subject type {subjectRequest.Key.GetType().FullName} is not supported for Request Type: {nameof(DeleteRequest)} and verifier token.");
                }
            }

            // Check verifier tasks to see if any had an error.
            await Task.WhenAll(deleteRequestAndVerifierResponseList.Select(c => c.GetVerifierTask)).ConfigureAwait(false);
            if (deleteRequestAndVerifierResponseList.Any(c => !c.GetVerifierTask.Result.IsSuccess))
            {
                // Log every error to assist in diagnosing issues, but just retun the first error
                foreach (DeleteRequestAndVerifierResponse deleteRequestAndVerifierResponse in deleteRequestAndVerifierResponseList)
                {
                    if (!deleteRequestAndVerifierResponse.GetVerifierTask.Result.IsSuccess)
                    {
                        var sllError = new ErrorEvent
                        {
                            ErrorName = "ErrorResponseFrom" + deleteRequestAndVerifierResponse.AdapterName,
                            ComponentName = nameof(PcfProxyService),
                            ErrorMethod = nameof(this.GetValidateAndAddVerifierTokenDeletesAsync),
                            ErrorCode = deleteRequestAndVerifierResponse.GetVerifierTask.Result.Error.Code.ToString(),
                            ErrorMessage = deleteRequestAndVerifierResponse.GetVerifierTask.Result.Error.Message
                        };
                        sllError.LogError();
                    }
                }

                return new AdapterResponse<IList<DeleteRequest>> { Error = deleteRequestAndVerifierResponseList.First(c => !c.GetVerifierTask.Result.IsSuccess).GetVerifierTask.Result.Error };
            }

            // Assign verifier tokens (if they are valid) to each request.
            // Verifier tokens are shared between requests, but each request still needs a token assigned to it individually.
            // This means that verifer tokens will be duplicated when a verifier token contains more than one command id.
            foreach (DeleteRequestAndVerifierResponse deleteRequestAndVerifierResponse in deleteRequestAndVerifierResponseList)
            {
                foreach (DeleteRequest deleteRequest in deleteRequestAndVerifierResponse.DeleteRequests)
                {
                    if (deleteRequest.Subject is AadSubject aadSubject)
                    {
                        if (this.aadRvsAdapter.TryGetOrgIdPuid(deleteRequestAndVerifierResponse.GetVerifierTask.Result.Result, out long orgIdPuid))
                        {
                            aadSubject.OrgIdPUID = orgIdPuid;
                        }
                        else
                        {
                            var adapterError = new AdapterError(AdapterErrorCode.BadRequest, "Failed to get OrgIdPuid", 500);
                            var sllError = new ErrorEvent
                            {
                                ErrorName = deleteRequestAndVerifierResponse.AdapterName + "FailedToGetOrgIdPuid",
                                ComponentName = nameof(PcfProxyService),
                                ErrorMethod = nameof(this.GetValidateAndAddVerifierTokenDeletesAsync),
                                ErrorCode = adapterError.Code.ToString(),
                                ErrorMessage = adapterError.Message
                            };
                            sllError.LogError();

                            return new AdapterResponse<IList<DeleteRequest>> { Error = adapterError };
                        }
                    }

                    AdapterResponse isValidVerifier =
                        await this.verificationTokenValidationService.ValidateVerifierAsync(deleteRequest, deleteRequestAndVerifierResponse.GetVerifierTask.Result.Result)
                            .ConfigureAwait(false);

                    if (isValidVerifier.IsSuccess)
                    {
                        // Assign the verifier token to the delete request
                        deleteRequest.VerificationToken = deleteRequestAndVerifierResponse.GetVerifierTask.Result.Result;
                    }
                    else
                    {
                        var sllError = new ErrorEvent
                        {
                            ErrorName = deleteRequestAndVerifierResponse.AdapterName + "VerifierFailedVerification",
                            ComponentName = nameof(PcfProxyService),
                            ErrorMethod = nameof(this.GetValidateAndAddVerifierTokenDeletesAsync),
                            ErrorCode = isValidVerifier.Error.Code.ToString(),
                            ErrorMessage = isValidVerifier.Error.Message
                        };
                        sllError.LogError();

                        return new AdapterResponse<IList<DeleteRequest>> { Error = isValidVerifier.Error };
                    }
                }
            }

            // Need to return the list because the original one was not modified.
            return new AdapterResponse<IList<DeleteRequest>> { Result = deleteRequestAndVerifierResponseList.SelectMany(c => c.DeleteRequests).ToList() };
        }

        private async Task<AdapterResponse> GetValidateAndAddVerifierTokenExportAsync(ExportRequest request, IRequestContext requestContext)
        {
            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();

            if (request.Subject is MsaSubject msaSubject)
            {
                AdapterResponse<string> getVerifierResponse =
                    await this.msaIdentityServiceAdapter.GetGdprExportVerifierAsync(request.RequestId, pxfRequestContext, request.StorageUri, msaSubject.Xuid)
                        .ConfigureAwait(false);
                if (!getVerifierResponse.IsSuccess)
                {
                    return new AdapterResponse { Error = getVerifierResponse.Error };
                }

                AdapterResponse isValidVerifier = await this.verificationTokenValidationService.ValidateVerifierAsync(request, getVerifierResponse.Result).ConfigureAwait(false);
                if (isValidVerifier.IsSuccess)
                {
                    request.VerificationToken = getVerifierResponse.Result;
                }
                else
                {
                    return isValidVerifier;
                }
            }
            else if (request.Subject is AadSubject aadSubject)
            {
                var aadRvsRequest = new AadRvsRequest
                {
                    TenantId = aadSubject.TenantId.ToString(),
                    ObjectId = aadSubject.ObjectId.ToString(),
                    StoragePath = request.StorageUri.ToString(),
                    ControllerApplicable = request.ControllerApplicable,
                    ProcessorApplicable = request.ProcessorApplicable,
                    CorrelationId = LogicalWebOperationContext.ServerActivityId.ToString(),
                    CommandIds = request.RequestId.ToString()
                };

                AdapterResponse<AadRvsVerifiers> aadRvsExportVerifier = await this.aadRvsAdapter.ConstructExportAsync(aadRvsRequest, requestContext).ConfigureAwait(false);
                if (!aadRvsExportVerifier.IsSuccess)
                {
                    this.logger.Error(nameof(PcfProxyService), $"GetValidateAndAddVerifierTokenExportAsync: Error: ({aadRvsExportVerifier.Error})");
                    return new AdapterResponse { Error = aadRvsExportVerifier.Error };
                }

                if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration).ConfigureAwait(false))
                {
                    var error = this.aadRvsAdapter.UpdatePrivacyRequestWithVerifiers(request, aadRvsExportVerifier.Result);
                    if (error != null)
                    {
                        this.logger.Error(nameof(PcfProxyService), $"UpdatePrivacyRequestWithVerifiers: Code: {error.Code}. Message: {error.Message}.");
                        return new AdapterResponse { Error = error };
                    }
                }
                else
                {
                    // TODO: Remove once the feature flag is permanently turned on
                    if (string.IsNullOrEmpty(aadRvsExportVerifier.Result.V2))
                    {
                        return new AdapterResponse { Error = new AdapterError(AdapterErrorCode.NullVerifier, "Missing V2 verifier", 0) };
                    }

                    request.VerificationToken = aadRvsExportVerifier.Result.V2;

                    if (this.aadRvsAdapter.TryGetOrgIdPuid(aadRvsExportVerifier.Result.V2, out long orgIdPuid))
                    {
                        aadSubject.OrgIdPUID = orgIdPuid;
                    }
                }

                // Validate non-empty token
                if (!string.IsNullOrEmpty(request.VerificationToken))
                {
                    AdapterResponse isValidVerifier = await this.verificationTokenValidationService.ValidateVerifierAsync(request, request.VerificationToken).ConfigureAwait(false);

                    if (!isValidVerifier.IsSuccess)
                    {
                        return new AdapterResponse { Error = isValidVerifier.Error };
                    }
                }

                if (!string.IsNullOrEmpty(request.VerificationTokenV3))
                {
                    AdapterResponse isValidVerifier = await this.verificationTokenValidationService.ValidateVerifierAsync(request, request.VerificationTokenV3).ConfigureAwait(false);

                    if (!isValidVerifier.IsSuccess)
                    {
                        return new AdapterResponse { Error = isValidVerifier.Error };
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"Subject type {request.Subject.GetType().FullName} not supported for verifier token.");
            }

            return new AdapterResponse();
        }

        /// <summary>
        ///     Get Verifier and Validate it for an AccountCleanup request. 
        /// </summary>
        /// <param name="request">AccountClose request.</param>
        /// <param name="requestContext">Request context</param>
        /// <returns>AadRvsAdapter Response</returns>
        private async Task<AdapterResponse> GetValidateAndAddVerifierTokenAccountCleanupAsync(AccountCloseRequest request, IRequestContext requestContext)
        {
            if (request.Subject is AadSubject)
            {
                AadRvsRequest aadRvsRequest = PrivacyRequestConverter.CreateAadRvsGdprRequestV2(request, AadRvsOperationType.AccountCleanup);

                AdapterResponse<AadRvsVerifiers> aadRvsAccountCleanupVerifier = await this.aadRvsAdapter.ConstructAccountCleanupAsync(aadRvsRequest, requestContext).ConfigureAwait(false);
                if (!aadRvsAccountCleanupVerifier.IsSuccess)
                {
                    this.logger.Error(nameof(PcfProxyService), $"ConstructAccountCleanupAsync Failed: {aadRvsAccountCleanupVerifier.Error}.");
                    return new AdapterResponse { Error = aadRvsAccountCleanupVerifier.Error };
                }

                var error = this.aadRvsAdapter.UpdatePrivacyRequestWithVerifiers(request, aadRvsAccountCleanupVerifier.Result);
                if (error != null)
                {
                    return new AdapterResponse { Error = error };
                }

                // There should be no V2 verifier
                if (!string.IsNullOrEmpty(request.VerificationToken))
                {
                    return new AdapterResponse { Error = new AdapterError(AdapterErrorCode.UnexpectedVerifier, "AccountCleanup should not have V2 verifier", 0) };
                }

                // Validate non-empty token
                if (!string.IsNullOrEmpty(request.VerificationTokenV3))
                {
                    AdapterResponse isValidVerifier = await this.verificationTokenValidationService.ValidateVerifierAsync(request, request.VerificationTokenV3).ConfigureAwait(false);

                    if (!isValidVerifier.IsSuccess)
                    {
                        return new AdapterResponse { Error = isValidVerifier.Error };
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"Subject type {request.Subject.GetType().FullName} not supported for verifier token.");
            }

            return new AdapterResponse();
        }

        private async Task<ServiceResponse<PrivacyRequestStatus>> ListRequestAsync(string callerId, Guid commandId, Task<AdapterResponse<CommandStatusResponse>> getRequest)
        {
            AdapterResponse<CommandStatusResponse> pcfResponse =
                await getRequest.ConfigureAwait(false);
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<PrivacyRequestStatus> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            // ******************************* DANGER *******************************
            // This if check is *extremely* important! It is what stops tenant admins from looking up the results for *other* tenants.
            // This *must* not be broken, or we will have very bad data leak issue!!!
            // ******************************* DANGER *******************************
            if (pcfResponse.Result != null && pcfResponse.Result.Requester != callerId)
            {
                this.logger.Warning(nameof(PcfProxyService), $"CallerId {callerId} was trying to read someone else's commandId ({commandId}), no result returned");
                return new ServiceResponse<PrivacyRequestStatus> { Result = null };
            }

            // ******************************* DANGER *******************************

            PrivacyRequestStatus transformedRequest = pcfResponse.Result == null ? null : ToPrivacyRequestStatus(pcfResponse.Result);

            return new ServiceResponse<PrivacyRequestStatus> { Result = transformedRequest };
        }

        private async Task<ServiceResponse<IList<PrivacyRequestStatus>>> ListRequestsAsync(Task<AdapterResponse<IList<CommandStatusResponse>>> getRequests)
        {
            AdapterResponse<IList<CommandStatusResponse>> pcfResponse =
                await getRequests.ConfigureAwait(false);
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse<IList<PrivacyRequestStatus>> { Error = new Error(ErrorCode.PartnerError, pcfResponse.Error.ToString()) };
            }

            List<PrivacyRequestStatus> transformedRequests = pcfResponse.Result
                .Where(r => !string.Equals(r.CommandType, "AccountClose", StringComparison.OrdinalIgnoreCase))
                .Select(ToPrivacyRequestStatus)
                .ToList();

            return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = transformedRequests };
        }

        private Error ThrottleCaller(IRequestContext requestContext, string apiKey)
        {
            string throttleName;
            string tenantId;
            string subjectId;
            IRequestThrottler throttler;

            Portal portal = requestContext.GetPortal(this.privacyConfigurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);
            switch (portal)
            {
                // PRC or CSS (AAD caller but against an MSA for posting)
                case Portal.Pcd:
                    throttleName = "PCD";
                    tenantId = requestContext.RequireIdentity<AadIdentity>().ApplicationId; // PCD

                    if (requestContext.Identity is AadIdentityWithMsaUserProxyTicket aadIdentityWithMsa)
                    {
                        // If this is a request with a proxy ticket, the subject is the MSA Puid
                        subjectId = aadIdentityWithMsa.TargetPuid.ToString();
                    }
                    else
                    {
                        // Otherwise it's an AltSubject or an Employee subject, and so we don't have a subjectId
                        // In theory we might be able to pass it through, but for now we will not do subject-level throttles,
                        // which is fine since we'll still be throttled by overall calls for the tenant (PCD in this case)
                        subjectId = null;
                    }

                    break;

                // MSGraph & Office (AAD authorizing as the tenant admin or a viral user)
                case Portal.MsGraph:
                case Portal.PxsAadTest:
                    throttleName = "MSGraph";
                    tenantId = requestContext.RequireIdentity<AadIdentity>().TenantId.ToString(); // AAD Tenant
                    subjectId = requestContext.RequireIdentity<AadIdentity>().ObjectId.ToString(); // AAD Subject
                    break;

                // AMC (MSA caller against an MSA)
                case Portal.Amc:
                case Portal.PxsTest:
                    throttleName = "AMC";
                    tenantId = "AMC";
                    subjectId = requestContext.RequireIdentity<MsaSelfIdentity>().AuthorizingPuid.ToString(); // MSA Subject
                    break;

                // Bing
                case Portal.Bing:
                    throttleName = "Bing";
                    tenantId = requestContext.RequireIdentity<AadIdentity>().ApplicationId; // Bing AAD AppId
                    subjectId = requestContext.RequireIdentity<AadIdentityWithMsaUserProxyTicket>().TargetPuid.ToString(); // MSA Subject
                    break;

                default:
                    throw new ArgumentOutOfRangeException(portal.ToString());
            }

            switch (apiKey)
            {
                case AccountCloseApiName:
                case ExportApiName:
                case DeleteApiName:
                    if (this.callerIdThrottles.TryGetValue($"{throttleName}_Post", out throttler) && throttler.ShouldThrottle(tenantId))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:AA)");
                    }

                    if (subjectId != null && this.callerIdThrottles.TryGetValue($"{throttleName}_Subject_Post", out throttler) &&
                        throttler.ShouldThrottle($"{tenantId}_{subjectId}"))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:AB)");
                    }

                    break;
                case ListByCallerApiName:
                case ListByUserApiName:
                    if (this.callerIdThrottles.TryGetValue($"{throttleName}_List", out throttler) && throttler.ShouldThrottle(tenantId))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:BA)");
                    }

                    if (subjectId != null && this.callerIdThrottles.TryGetValue($"{throttleName}_Subject_List", out throttler) &&
                        throttler.ShouldThrottle($"{tenantId}_{subjectId}"))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:BB)");
                    }

                    break;
                case LookupByIdApiName:
                    if (this.callerIdThrottles.TryGetValue($"{throttleName}_Lookup", out throttler) && throttler.ShouldThrottle(tenantId))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:CA)");
                    }

                    if (subjectId != null && this.callerIdThrottles.TryGetValue($"{throttleName}_Subject_Lookup", out throttler) &&
                        throttler.ShouldThrottle($"{tenantId}_{subjectId}"))
                    {
                        return new Error(ErrorCode.TooManyRequests, "Request throttled (Code:CB)");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(apiKey));
            }

            return null;
        }

        internal class DeleteRequestAndVerifierResponse
        {
            internal IList<DeleteRequest> DeleteRequests { get; }

            internal Task<AdapterResponse<string>> GetVerifierTask { get; }

            internal string AdapterName { get; }

            internal DeleteRequestAndVerifierResponse(IList<DeleteRequest> deleteRequests, Task<AdapterResponse<string>> getVerifierTask, string adapterName)
            {
                this.DeleteRequests = deleteRequests;
                this.GetVerifierTask = getVerifierTask;
                this.AdapterName = adapterName;
            }
        }

        private static ListActorAuthorization ConvertAadRvsScopeResponse(AadRvsScopeResponse aadRvsScopeResponse)
        {
            if (!string.Equals(aadRvsScopeResponse.Outcome, AadRvsOutcome.OperationSuccess.ToString())
                && !string.Equals(aadRvsScopeResponse.Outcome, AadRvsOutcome.OperationSuccessPractice.ToString()))
            {
                return ListActorAuthorization.Unauthorized;
            }

            if (!string.IsNullOrEmpty(aadRvsScopeResponse.Scopes))
            {
                if (aadRvsScopeResponse.Scopes.Contains(AadRvsScope.UserProcesscorExportAll))
                {
                    return ListActorAuthorization.ListAll;
                }

                if (aadRvsScopeResponse.Scopes.Contains(AadRvsScope.UserProcessorExport))
                {
                    return ListActorAuthorization.ListActorOnly;
                }
            }

            return ListActorAuthorization.Unauthorized;
        }

        private static Dictionary<IPrivacySubject, List<DeleteRequest>> CreateSubjectRequestDictionary(IList<DeleteRequest> pcfDeleteRequests)
        {
            var subjectDictionary = new Dictionary<IPrivacySubject, List<DeleteRequest>>();

            foreach (KeyValuePair<IPrivacySubject, IEnumerable<DeleteRequest>> request in pcfDeleteRequests
                .Where(c => c.Subject is MsaSubject)
                .GroupBy(c => ((MsaSubject)c.Subject).Puid)
                .ToDictionary(r => r.Select(c => c.Subject).First(), r => r.Select(c => c)))
            {
                subjectDictionary.Add(request.Key, request.Value.ToList());
            }

            foreach (KeyValuePair<IPrivacySubject, IEnumerable<DeleteRequest>> request in pcfDeleteRequests
                .Where(c => c.Subject is DeviceSubject)
                .GroupBy(c => ((DeviceSubject)c.Subject).GlobalDeviceId)
                .ToDictionary(r => r.Select(c => c.Subject).First(), r => r.Select(c => c)))
            {
                subjectDictionary.Add(request.Key, request.Value.ToList());
            }

            foreach (KeyValuePair<IPrivacySubject, IEnumerable<DeleteRequest>> request in pcfDeleteRequests
                .Where(c => c.Subject is AadSubject)
                .GroupBy(c => ((AadSubject)c.Subject).ObjectId)
                .ToDictionary(r => r.Select(c => c.Subject).First(), r => r.Select(c => c)))
            {
                subjectDictionary.Add(request.Key, request.Value.ToList());
            }

            foreach (KeyValuePair<IPrivacySubject, IEnumerable<DeleteRequest>> request in pcfDeleteRequests
                .Where(c => c.Subject is MicrosoftEmployee)
                .GroupBy(c => ((MicrosoftEmployee)c.Subject).EmployeeId)
                .ToDictionary(r => r.Select(c => c.Subject).First(), r => r.Select(c => c)))
            {
                subjectDictionary.Add(request.Key, request.Value.ToList());
            }

            foreach (KeyValuePair<IPrivacySubject, IEnumerable<DeleteRequest>> request in pcfDeleteRequests
                .Where(c => c.Subject is DemographicSubject)
                .GroupBy(c => c.Subject)
                .ToDictionary(r => r.Select(c => c.Subject).First(), r => r.Select(c => c)))
            {
                subjectDictionary.Add(request.Key, request.Value.ToList());
            }

            return subjectDictionary;
        }

        private static ServiceResponse<T> HandleAadRvsAdapterError<T>(AdapterError adapterError)
        {
            ErrorCode errorCode = adapterError.Code.ToServiceErrorCode();
            return new ServiceResponse<T> { Error = new Error(errorCode, adapterError.Message) };
        }

        private static async Task<ServiceResponse<Guid>> ValidateExternalStorageAsync(ExportRequest request, ILogger logger)
        {
            try
            {
                // Does the Uri parse into an Azure Blob Container with a SAS token
                var testContainer = new CloudBlobContainer(request.StorageUri);
                if (testContainer.ServiceClient?.Credentials?.IsSAS != true)
                {
                    return new ServiceResponse<Guid> { Error = new Error(ErrorCode.SharedAccessSignatureTokenInvalid, GraphApiErrorMessage.SharedAccessSignatureTokenInvalid) };
                }

                // We have avoided trying to parse these query parameters so far, but unfortunately there seems to be no other way to distinguish if we
                // are given an Account SAS (not supported) or a Service SAS (what we need). So here we will attempt to distinguish based on the documentation
                // described here: https://docs.microsoft.com/en-us/azure/storage/common/storage-dotnet-shared-access-signature-part-1
                // Namely, if there is a 'ss' query parameter we will assume an Account SAS.
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(request.StorageUri.Query);
                if (queryParameters.Get("ss") != null)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationNotServiceSAS, GraphApiErrorMessage.StorageLocationNotServiceSAS)
                    };
                }

                // Also verify we are pointing at a container (just a single component to the local path)
                string[] pathComponents = request.StorageUri.LocalPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (pathComponents.Length != 1)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationNotAzureBlob, GraphApiErrorMessage.StorageLocationNotAzureBlob)
                    };
                }

                // Is listing blobs forbidden (no list access)
                try
                {
                    await testContainer.ListBlobsSegmentedAsync(null).ConfigureAwait(false);
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationShouldNotAllowListAccess, GraphApiErrorMessage.StorageLocationShouldNotAllowListAccess)
                    };
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Forbidden)
                {
                    // Only acceptable exception is Forbidden.
                }

                // Attempt to write request metadata
                try
                {
                    CloudAppendBlob blob = testContainer.GetAppendBlobReference(RequestInfoBlobName);
                    var info = new
                    {
                        Description = RequestInfoDescription,
                        CorrelationId = request.CorrelationVector,
                        CommandId = request.RequestId,
                        request.Subject,
                        request.Timestamp,
                        request.RequestGuid

                        // This is no longer included per Bug 19348956: Update Text in RequestInfo.json
                        // We'll probably re-enable this if scenarios get exposed where data types are more variable
                        ////DataTypes = request.PrivacyDataTypes,
                    };
                    await blob.UploadTextAsync(
                        JsonConvert.SerializeObject(info, Formatting.Indented),
                        Encoding.UTF8,
                        AccessCondition.GenerateIfNotExistsCondition(),
                        null,
                        null).ConfigureAwait(false);
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    // If there is a conflict, the request info is already there, which means this destination has already been used.
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationAlreadyUsed, GraphApiErrorMessage.StorageLocationAlreadyUsed)
                    };
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Forbidden)
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationNeedsWriteAddPermissions, GraphApiErrorMessage.StorageLocationNeedsWriteAddPermissions)
                    };
                }
                catch (StorageException ex) when (ex.RequestInformation.ErrorCode == "BlobTypeNotSupported") // NOT! BlobErrorCodeStrings.InvalidBlobType
                {
                    return new ServiceResponse<Guid>
                    {
                        Error = new Error(ErrorCode.StorageLocationShouldSupportAppendBlobs, GraphApiErrorMessage.StorageLocationShouldSupportAppendBlobs)
                    };
                }

                // Verify reading blobs is forbidden
                try
                {
                    await testContainer.GetBlobReference(RequestInfoBlobName).DownloadRangeToByteArrayAsync(new byte[1], 0, 0, 1).ConfigureAwait(false);
                    try
                    {
                        // Make a best effort to clean up the request info blob so the caller can use the same container after fixing the permissions
                        await testContainer.GetBlobReference(RequestInfoBlobName).DeleteAsync().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Do the best we can, but if they give read, write, and not delete, we can't clean up after ourselves.
                    }

                    return new ServiceResponse<Guid>
                    { Error = new Error(ErrorCode.StorageLocationShouldNotAllowReadAccess, GraphApiErrorMessage.StorageLocationShouldNotAllowReadAccess) };
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Forbidden)
                {
                    // Only acceptable exception is Forbidden.
                }
            }
            catch (Exception ex)
            {
                // If we get any other exception, it's not a good destination
                logger.Error(nameof(PcfProxyService), ex, GraphApiErrorMessage.StorageLocationInvalid);
                return new ServiceResponse<Guid> { Error = new Error(ErrorCode.StorageLocationInvalid, GraphApiErrorMessage.StorageLocationInvalid) };
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<ServiceResponse> PostMsaRecurringDeleteRequestsAsync(IRequestContext requestContext, DeleteRequest deleteRequest, string preverifier)
        {
            if (deleteRequest == null)
            {
                return new ServiceResponse { Error = new Error(ErrorCode.InvalidInput, "Delete request is null.") };
            }

            if (!(deleteRequest.Subject is MsaSubject))
            {
                return new ServiceResponse { Error = new Error(ErrorCode.InvalidInput, "The subject should be Msa subject.") };
            }

            var msaSubject = (MsaSubject)deleteRequest.Subject;
            if (msaSubject.Puid != requestContext.TargetPuid)
            {
                return new ServiceResponse { Error = new Error(ErrorCode.Unauthorized, "Cannot submit MSA subject different than the current user") };
            }

            AdapterResponse<Dictionary<long, string>> xuidResponse =
                await this.xboxAccountsAdapter.GetXuidsAsync(new List<long> { requestContext.TargetPuid }).ConfigureAwait(false);
            if (!xuidResponse.IsSuccess)
            {
                return new ServiceResponse { Error = new Error(ErrorCode.PartnerError, xuidResponse.Error.ToString()) };
            }

            if (!xuidResponse.Result.TryGetValue(requestContext.TargetPuid, out string xuid))
            {
                return new ServiceResponse { Error = new Error(ErrorCode.PartnerError, $"Cannot retrieve XUID for PUID {requestContext.TargetPuid}") };
            }

            // Complete the MSA subjects with a xuid
            msaSubject.Xuid = xuid;

            // Get verifier
            Task<AdapterResponse<string>> msaTask = this.msaIdentityServiceAdapter.GetGdprUserDeleteVerifierWithPreverifierAsync(
                        deleteRequest.RequestId,
                        requestContext.ToAdapterRequestContext(),
                        preverifier,
                        msaSubject.Xuid,
                        predicate: null,
                        dataType: deleteRequest.PrivacyDataType);
            await msaTask.ConfigureAwait(false);

            if (!msaTask.Result.IsSuccess)
            {
                var sllError = new ErrorEvent
                {
                    ErrorName = "ErrorResponseFrom" + nameof(msaIdentityServiceAdapter),
                    ComponentName = nameof(PcfProxyService),
                    ErrorMethod = nameof(this.PostMsaRecurringDeleteRequestsAsync),
                    ErrorCode = msaTask.Result.Error.Code.ToString(),
                    ErrorMessage = msaTask.Result.Error.Message
                };
                sllError.LogError();
                return new ServiceResponse { Error = new Error(ErrorCode.Unauthorized, msaTask.Result.Error.Message) };
            }

            // validate the verifier
            AdapterResponse isValidVerifier =
                       await this.verificationTokenValidationService.ValidateVerifierAsync(deleteRequest, msaTask.Result.Result)
                           .ConfigureAwait(false);

            if (isValidVerifier.IsSuccess)
            {
                var verifier = msaTask.Result.Result;
                long cidValue = 0;
                if (!string.IsNullOrEmpty(verifier))
                {
                    cidValue = MsaIdentityServiceAdapter.GetCidFromVerifier(verifier);
                }

                // update cid from verifier
                (deleteRequest.Subject as MsaSubject).Cid = cidValue;                
                
                // Assign the verifier token to the delete request
                deleteRequest.VerificationToken = msaTask.Result.Result;
            }
            else
            {
                var sllError = new ErrorEvent
                {
                    ErrorName = nameof(msaIdentityServiceAdapter) + "VerifierFailedVerification",
                    ComponentName = nameof(PcfProxyService),
                    ErrorMethod = nameof(this.PostMsaRecurringDeleteRequestsAsync),
                    ErrorCode = isValidVerifier.Error.Code.ToString(),
                    ErrorMessage = isValidVerifier.Error.Message
                };
                sllError.LogError();

                return new ServiceResponse { Error = new Error(ErrorCode.Unauthorized, isValidVerifier.Error.Message) };
            }

            // Write PCF requests to PCF
            AdapterResponse pcfResponse = await this.pcfAdapter.PostCommandsAsync(new PrivacyRequest[] { deleteRequest }).ConfigureAwait(false);
            // If write fails, fail the whole request
            if (!pcfResponse.IsSuccess)
            {
                return new ServiceResponse
                {
                    Error = new Error
                    {
                        Code = pcfResponse.Error.StatusCode.ToString(),
                        Message = pcfResponse.Error.Message
                    }
                };
            }

            return new ServiceResponse();
        }

        public async Task<ServiceResponse> DeleteExportsAsync(DeleteExportArchiveParameters parameters)
        {
            ServiceResponse serviceResponse = new ServiceResponse();
            AdapterResponse response = await this.pcfAdapter.DeleteExportArchiveAsync(parameters).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                serviceResponse.Error = new Error(response.Error.Code.ToServiceErrorCode(), response.Error.ToString());
            }
            return serviceResponse;
        }
    }
}
