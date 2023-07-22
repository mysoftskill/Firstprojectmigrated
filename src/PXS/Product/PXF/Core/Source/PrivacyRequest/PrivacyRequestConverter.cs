// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using static Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.AadRequestVerificationServiceAdapter;
    using IdConverter = SocialAccessorV4.IdConverter;
    using PcfPrivacySubjects = Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    ///     Public static class PrivacyCommandConverter.
    /// </summary>
    public static class PrivacyRequestConverter
    {
        private const string HexFormatter = "X16";

        // TODO: First stab from john, this should be config driven.
        private static readonly string[] defaultExportDataTypes = 
        {
            Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
            Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value,
            Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
            Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value,
            Policies.Current.DataTypes.Ids.ContentConsumption.Value,
            Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value,
            Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory.Value,
            Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration.Value,
            Policies.Current.DataTypes.Ids.DemographicInformation.Value,
            Policies.Current.DataTypes.Ids.FeedbackAndRatings.Value,
            Policies.Current.DataTypes.Ids.FitnessAndActivity.Value,
            Policies.Current.DataTypes.Ids.InterestsAndFavorites.Value,
            Policies.Current.DataTypes.Ids.SupportContent.Value,
            Policies.Current.DataTypes.Ids.SupportInteraction.Value,
            Policies.Current.DataTypes.Ids.EnvironmentalSensor.Value,
            Policies.Current.DataTypes.Ids.Social.Value,
            Policies.Current.DataTypes.Ids.EUII.Value,
            Policies.Current.DataTypes.Ids.Support.Value,
            Policies.Current.DataTypes.Ids.Account.Value,
            Policies.Current.DataTypes.Ids.PublicPersonal.Value,
            Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value
        };

        private static readonly string[] dataTypesExcludedForAAD =
        {
            Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value
        };

        private static readonly string[] defaultAADExportDataTypes = defaultExportDataTypes.Where(
            dataType => !dataTypesExcludedForAAD.Contains(dataType)).ToArray();


        /// <summary>
        ///     Convert a PrivacyRequest to a request with a device subject. Used for device expansion.
        /// </summary>
        public static PrivacyRequest AsDeviceRequest(this PrivacyRequest request, string globalDeviceId)
        {
            PrivacyRequest newRequest = request.ShallowCopyWithNewId();
            newRequest.Subject = CreateDeviceSubject(RemoveGlobalFormat(globalDeviceId));
            return newRequest;
        }

        /// <summary>
        ///     Creates an export request in an aad context.
        /// </summary>
        public static ExportRequest CreateAadExportRequest(
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Uri storageUri,
            bool isSynthetic,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            PcfPrivacySubjects.AadSubject aadSubject = CreateAadSubjectFromIdentity(requestContext.RequireIdentity<AadIdentity>());

            return CreateExportRequest(
                aadSubject,
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                storageUri,
                defaultAADExportDataTypes,
                isSynthetic,
                cloudInstance,
                portal,
                isTest);
        }

        /// <summary>
        ///     Creates an AAD RVS request
        /// </summary>
        /// <param name="accountCloseRequest">The account close request</param>
        /// <param name="operationType">The account close operation type - AccountClose or AccountCleanup</param>
        /// <returns>An AAD RVS request</returns>
        public static AadRvsRequest CreateAadRvsGdprRequestV2(AccountCloseRequest accountCloseRequest, AadRvsOperationType operationType)
        {
            switch (accountCloseRequest.Subject)
            {
                case PcfPrivacySubjects.AadSubject aadSubject:
                    UpdateRequestApplicabilityForAccountClose(accountCloseRequest, operationType);
                    return new AadRvsRequest
                    {
                        TenantId = aadSubject.TenantId.ToString(),
                        ObjectId = aadSubject.ObjectId.ToString(),
                        Operation = operationType.ToString(),
                        ControllerApplicable = accountCloseRequest.ControllerApplicable,
                        ProcessorApplicable = accountCloseRequest.ProcessorApplicable,
                        CorrelationId = accountCloseRequest.RequestGuid.ToString(),
                        CommandIds = accountCloseRequest.RequestId.ToString(),
                        PreVerifier = accountCloseRequest.VerificationToken,

                        // AAD RVS expects a valid value for OrgIdPuid, or empty string. But not null.
                        OrgIdPuid = aadSubject.OrgIdPUID != 0 ? aadSubject.OrgIdPUID.ToString(HexFormatter) : string.Empty
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(accountCloseRequest.Subject), $"Subject type not supported: {accountCloseRequest.Subject?.GetType().Name}.");
            }
        }

        /// <summary>
        ///     Create export request
        /// </summary>
        public static ExportRequest CreateExportRequest(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            string context,
            string verificationToken,
            Uri storageUri,
            IList<string> dataTypes,
            bool isSynthetic,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            var exportRequest = new ExportRequest
            {
                StorageUri = storageUri,
                PrivacyDataTypes = dataTypes ?? defaultExportDataTypes,
                IsSyntheticRequest = isSynthetic,
            };
            PopulateCommonPcfRequestFields(
                exportRequest,
                RequestType.Export,
                subject,
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                context,
                verificationToken,
                cloudInstance,
                portal,
                isTest);
            return exportRequest;
        }

        /// <summary>
        ///     Create a PCF Account Close request.
        /// </summary>
        public static AccountCloseRequest CreatePcfAccountCloseRequest(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timeStamp,
            string cloudInstance,
            string portal,
            string preverifier,
            bool isTest)
        {
            AccountCloseRequest request = CreateAccountCloseRequest(
                subject,
                requestContext,
                requestGuid,
                correlationVector,
                timeStamp,
                context: null,
                cloudInstance: cloudInstance,
                portal: portal,
                isTest: isTest);

            request.VerificationToken = preverifier;
            return request;
        }

        /// <summary>
        ///     Creates PCF delete requests.
        /// </summary>
        public static IEnumerable<DeleteRequest> CreatePcfDeleteRequests(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            string context,
            DateTimeOffset timestamp,
            IEnumerable<string> privacyDataTypes,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            return privacyDataTypes.Select(
                dt => CreateDeleteRequest(
                    subject,
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    context,
                    null,
                    null,
                    new TimeRangePredicate
                    {
                        StartTime = startTime,
                        EndTime = endTime
                    },
                    dt,
                    cloudInstance,
                    portal,
                    isTest));
        }

        /// <summary>
        ///     Creates PCF delete requests.
        /// </summary>
        public static IEnumerable<DeleteRequest> CreatePcfDeleteRequests(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            string context,
            DateTimeOffset timestamp,
            IEnumerable<IPrivacyPredicate> predicates,
            string privacyDataType,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (predicates == null || !predicates.Any())
            {
                // Bulk delete all records for data type
                return new[]{ CreateDeleteRequest(
                    subject,
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    context,
                    null,
                    null,
                    new TimeRangePredicate
                    {
                        StartTime = startTime,
                        EndTime = endTime
                    },
                    privacyDataType,
                    cloudInstance,
                    portal,
                    isTest)};
            }
            else
            {
                // Single deletes
                return predicates.Select(
                    predicate => CreateDeleteRequest(
                        subject,
                        requestContext,
                        requestGuid,
                        correlationVector,
                        timestamp,
                        context,
                        null,
                        predicate,
                        new TimeRangePredicate
                        {
                            StartTime = startTime,
                            EndTime = endTime
                        },
                        privacyDataType,
                        cloudInstance,
                        portal,
                        isTest));
            }
        }

        /// <summary>
        ///     Convert from Experience Contract subject to a Command Feed subject.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static PcfPrivacySubjects.IPrivacySubject ToSubject(IPrivacySubject subject, IRequestContext requestContext)
        {
            switch (subject)
            {
                case MsaSelfAuthSubject _:
                    return CreateMsaSubjectFromContext(requestContext);

                case DemographicSubject demographicSubject:
                    return CreateFromAltSubject(demographicSubject);

                case MicrosoftEmployeeSubject microsoftEmployeeSubject:
                    return CreateFromAltSubject(microsoftEmployeeSubject);
            }

            throw new NotSupportedException($"Subject type {subject.GetType().FullName} not supported");
        }

        public static PcfPrivacySubjects.IPrivacySubject ToSubject(
            PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.IPrivacySubject subject,
            IRequestContext requestContext)
        {
            switch (subject)
            {
                case PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject _:
                    return CreateMsaSubjectFromContext(requestContext);

                case PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject demographicSubject:
                    return CreateFromAltSubject(demographicSubject);

                case PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject microsoftEmployeeSubject:
                    return CreateFromAltSubject(microsoftEmployeeSubject);
            }

            throw new NotSupportedException($"Subject type {subject.GetType().FullName} not supported");
        }

        /// <summary>
        ///     Converts the multiple user delete requests for PCF <see cref="DeleteRequest" />.
        /// </summary>
        public static IEnumerable<DeleteRequest> ToUserPcfDeleteRequests(
            this IEnumerable<TimelineCard> cards,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            return cards.SelectMany(c => c.ToUserPcfDeleteRequests(requestContext, requestGuid, correlationVector, timestamp, policy, cloudInstance, portal, isTest));
        }

        /// <summary>
        ///     Gets the request applicability for if it is applicable to controllers/processors.
        /// </summary>
        /// <param name="request">The request.</param>
        public static void UpdateRequestApplicability(PrivacyRequest request)
        {
            request.ControllerApplicable = false;
            request.ProcessorApplicable = false;

            // https://microsoft.sharepoint.com/teams/osg_unistore/mem/mee/_layouts/OneNote.aspx?id=%2Fteams%2Fosg_unistore%2Fmem%2Fmee%2FShared%20Documents%2FPXS%2FPrivacy%20Command%20Feed%20Team&wd=target%28Design%20Specs.one%7C94748613-D2D1-4D1A-BEBF-F35EBB506B6B%2FPhase%20I%20-%20Hard%20coding%20the%20values%20in%20PXS%2C%20Frontloading%7C7E66CB2D-38FE-4555-9216-BA6942D24DAC%2F%29
            if (request is AccountCloseRequest || request is AgeOutRequest)
            {
                request.ControllerApplicable = true;
                request.ProcessorApplicable = true;
            }
            else
            {
                switch (request.Subject)
                {
                    case PcfPrivacySubjects.MsaSubject _:
                    case PcfPrivacySubjects.DeviceSubject _:
                        request.ControllerApplicable = true;
                        request.ProcessorApplicable = false;
                        break;
                    case PcfPrivacySubjects.AadSubject _:
                        request.ControllerApplicable = false;
                        request.ProcessorApplicable = true;
                        break;
                    default:
                        request.ControllerApplicable = true;
                        request.ProcessorApplicable = false;
                        break;
                }
            }
        }

        private static void UpdateRequestApplicabilityForAccountClose(AccountCloseRequest accountCloseRequest, AadRvsOperationType operationType)
        {
            // See detail about this decision here: https://microsoft.sharepoint-df.com/teams/NGPCommonInfra/_layouts/OneNote.aspx?id=%2Fteams%2FNGPCommonInfra%2FSiteAssets%2FNGP%20Common%20Infra%20Notebook&wd=target%28Design%20Specs.one%7CF5549725-2AB5-4D77-A94F-1CB521DED70E%2Fpa%20and%20ca%20for%20%27NI%20Close%27%20%5C%2F%20%27AccountCleanup%7CDEB7E3D5-F1B3-49BA-A0AB-BF43AC290D96%2F%29
            accountCloseRequest.ProcessorApplicable = true;
            accountCloseRequest.ControllerApplicable = (operationType == AadRvsOperationType.AccountCleanup) ? false : true;
        }

        /// <summary>
        ///     Creates the vortex device-delete-request.
        /// </summary>
        /// <param name="evt">The vortex event.</param>
        /// <param name="serverTime">The server time.</param>
        /// <param name="signalData">The signal information to generate the delete request from</param>
        /// <param name="policy">The policy</param>
        /// <returns>The delete request</returns>
        internal static DeleteRequest CreateVortexDeviceDeleteRequest(
            VortexEvent evt,
            DateTimeOffset serverTime,
            SignalData signalData,
            Policy policy)
        {
            var deleteRequest = new DeleteRequest
            {
                AuthorizationId = evt.Ext?.User?.Id ?? evt.LegacyUserId,
                RequestId = signalData.CommandId,
                RequestType = RequestType.Delete,
                Subject = new PcfPrivacySubjects.DeviceSubject { GlobalDeviceId = DeviceIdParser.ParseDeviceIdAsInt64(evt.Ext?.Device?.Id ?? evt.LegacyDeviceId) },
                PrivacyDataType = signalData.PrivacyDataType,
                Predicate = CreatePrivacyPredicate(signalData.PrivacyDataType, policy),
                Timestamp = serverTime,
                RequestGuid = signalData.RequestGuid,
                CorrelationVector = evt.CorrelationVector ?? evt.Tags?.CorrelationVector,
                TimeRangePredicate = new TimeRangePredicate { EndTime = evt.Time },
                VerificationToken = signalData.VerifierToken,
                Requester = "Vortex",
                Portal = Portals.VortexDeviceDeleteSignal
            };

            UpdateRequestApplicability(deleteRequest);
            return deleteRequest;
        }

        /// <summary>
        ///     Creates the anaheim delete-device-id-request.
        /// </summary>
        /// <param name="evt">The vortex event.</param>
        /// <param name="requestId">The request id of DeviceDeleteRequest</param>
        /// <param name="requestTime"></param>
        /// <param name="testSignal"></param>
        /// <returns>The delete-device-id request</returns>
        internal static DeleteDeviceIdRequest CreateAnaheimDeleteDeviceIdRequest(
            VortexEvent evt,
            Guid requestId,
            DateTimeOffset requestTime,
            bool testSignal = default)
        {
            var deleteDeviceIdRequest = new DeleteDeviceIdRequest
            {
                AuthorizationId = evt.Ext?.User?.Id,
                CorrelationVector = evt.CorrelationVector,
                RequestId = requestId,
                GlobalDeviceId = DeviceIdParser.ParseDeviceIdAsInt64(evt.Ext?.Device?.Id ?? evt.LegacyDeviceId),
                CreateTime = requestTime,
                TestSignal = testSignal
            };

            return deleteDeviceIdRequest;
        }

        /// <summary>
        ///     Creates the predicate.
        /// </summary>
        /// <param name="privacyDataType">Type of the privacy data.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>the predicate</returns>
        /// <exception cref="ArgumentOutOfRangeException">Received an unexpected privacy data type</exception>
        private static IPrivacyPredicate CreatePrivacyPredicate(string privacyDataType, Policy policy)
        {
            if (string.Equals(privacyDataType, policy.DataTypes.Ids.ProductAndServiceUsage.Value))
            {
                return new ProductAndServiceUsagePredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            if (string.Equals(privacyDataType, policy.DataTypes.Ids.ProductAndServicePerformance.Value))
            {
                return new ProductAndServicePerformancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            if (string.Equals(privacyDataType, policy.DataTypes.Ids.SoftwareSetupAndInventory.Value))
            {
                return new SoftwareSetupAndInventoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            if (string.Equals(privacyDataType, policy.DataTypes.Ids.BrowsingHistory.Value))
            {
                return new BrowsingHistoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            if (string.Equals(privacyDataType, policy.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value))
            {
                return new InkingTypingAndSpeechUtterancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            if (string.Equals(privacyDataType, policy.DataTypes.Ids.DeviceConnectivityAndConfiguration.Value))
            {
                return new DeviceConnectivityAndConfigurationPredicate { WindowsDiagnosticsDeleteOnly = true };
            }

            throw new ArgumentOutOfRangeException(nameof(privacyDataType));
        }

        private static AccountCloseRequest CreateAccountCloseRequest(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            string context,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            var accountCloseRequest = new AccountCloseRequest();
            PopulateCommonPcfRequestFields(
                accountCloseRequest,
                RequestType.AccountClose,
                subject,
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                context,
                verificationToken: null,
                cloudInstance: cloudInstance,
                portal: portal,
                isTest: isTest);
            return accountCloseRequest;
        }

        private static DeleteRequest CreateDeleteRequest(
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            string context,
            string verificationToken,
            IPrivacyPredicate predicate,
            TimeRangePredicate timeRangePredicate,
            string dataType,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            var deleteRequest = new DeleteRequest
            {
                Predicate = predicate,
                TimeRangePredicate = timeRangePredicate,
                PrivacyDataType = dataType,
            };
            PopulateCommonPcfRequestFields(
                deleteRequest,
                RequestType.Delete,
                subject,
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                context,
                verificationToken,
                cloudInstance,
                portal,
                isTest);
            return deleteRequest;
        }

        private static void PopulateCommonPcfRequestFields(
            PrivacyRequest request,
            RequestType requestType,
            PcfPrivacySubjects.IPrivacySubject subject,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            string context,
            string verificationToken,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            switch (requestContext.Identity)
            {
                case AadIdentity aadIdentity:
                    request.AuthorizationId = $"a:{aadIdentity.ObjectId}";
                    break;
                case MsaSelfIdentity msaIdentity:
                    request.AuthorizationId = $"p:{msaIdentity.AuthorizingPuid}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestContext.Identity), $"Unexpected identity type: {requestContext.Identity.GetType().FullName}");
            }

            request.Subject = subject;
            request.Context = context;
            request.CorrelationVector = correlationVector;
            request.IsWatchdogRequest = requestContext.IsWatchdogRequest;
            request.RequestGuid = requestGuid;
            request.RequestId = Guid.NewGuid();
            request.RequestType = requestType;
            request.Timestamp = timestamp;
            request.VerificationToken = verificationToken;

            // PCF owns what this string should be.
            // https://microsoft.visualstudio.com/Universal%20Store/MEE.Privacy.CommandFeed/_git/MEE.Privacy.CommandFeed.Service?path=%2FProduct%2FLibraries%2FPrivacyCommandValidator%2FConfiguration%2FCloudInstance.cs
            request.CloudInstance = cloudInstance;

            // The requester is either the AAD tenant or the MSA site caller name
            // Requester is used for later 'get requests by caller' calls, for example a tenant admin listing his requests (this why it's the tenant id)
            string requester = requestContext.GetIdentityValueOrDefault<AadIdentity, MsaSiteIdentity, string>(i => i.TenantId.ToString(), i => i.CallerName);

            // Except for PCD, where the requester is the application id. (All PCD requests show up together when getting a list of requests)
            if (portal == Portals.Pcd)
                requester = requestContext.GetIdentityValueOrDefault<AadIdentity, string>(i => i.ApplicationId.ToString());

            // For account closes and age outs, keep the requester different. We don't want AccountClose or AgeOut to show up in normal GetByRequester
            // calls for a tenant. Keeping the CallerId in the requester field though allows us to build such a status page if we
            // ever wanted to. For MSA Account closes, the requester is just 'Aqs' (See AccountDeleteInformation.ToAccountCloseRequest)
            switch (request)
            {
                case AccountCloseRequest _:
                    request.Requester = $"AccountClose_{requester}";
                    break;
                case AgeOutRequest _:
                    request.Requester = $"AgeOut_{requester}";
                    break;
                default:
                    request.Requester = requester;
                    break;
            }

            request.Portal = portal;
            request.IsTestRequest = isTest;

            UpdateRequestApplicability(request);
        }

        internal static string RemoveGlobalFormat(string globalDeviceId)
        {
            // DDS exposes the global device id in the format of global[{X16}], where the X16 is the device ID in 16-char upper case hexadecimal
            // Remove the globa[] part of the id

            const string GlobalPrefix = "global[";
            return globalDeviceId.StartsWith(GlobalPrefix)
                ? globalDeviceId.Substring(GlobalPrefix.Length, globalDeviceId.Length - GlobalPrefix.Length).TrimEnd(']')
                : globalDeviceId;
        }

        internal static DeleteRequest ToUserPcfDeleteRequest(
            this AppUsageCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            var predicate = new ProductAndServiceUsagePredicate
            {
                AppId = card.AppId
            };
            if (card.PropertyBag != null)
            {
                predicate.PropertyBag = new Dictionary<string, List<string>>();
                foreach (KeyValuePair<string, IList<string>> item in card.PropertyBag)
                {
                    predicate.PropertyBag[item.Key] = item.Value as List<string> ?? item.Value?.ToList();
                }
            }

            DeleteRequest to = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                predicate,
                new TimeRangePredicate
                {
                    StartTime = card.Timestamp.UtcDateTime,
                    EndTime = card.EndTimestamp.UtcDateTime
                },
                policy.DataTypes.Ids.ProductAndServiceUsage.Value,
                cloudInstance,
                portal,
                isTest);

            return to;
        }

        internal static DeleteRequest ToUserPcfDeleteRequest(
            this VoiceCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            DeleteRequest to = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                new InkingTypingAndSpeechUtterancePredicate
                {
                    ImpressionGuid = card.VoiceId
                },
                new TimeRangePredicate
                {
                    StartTime = card.Timestamp.UtcDateTime,
                    EndTime = card.Timestamp.UtcDateTime
                },
                policy.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value,
                cloudInstance,
                portal,
                isTest);

            return to;
        }

        private static DeleteRequest ToUserPcfDeleteRequest(
            this ContentConsumptionCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            DeleteRequest to = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                new ContentConsumptionPredicate
                {
                    ContentId = card.MediaId
                },
                null,
                policy.DataTypes.Ids.ContentConsumption.Value,
                cloudInstance,
                portal,
                isTest);

            return to;
        }

        private static DeleteRequest ToUserPcfDeleteRequest(
            this BrowseCard.Navigation navigation,
            DateTimeOffset navigationTimestamp,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (navigation == null)
                throw new ArgumentNullException(nameof(navigation));

            DeleteRequest deleteRequest = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                new BrowsingHistoryPredicate { UriHash = navigation.UriHash },
                new TimeRangePredicate
                {
                    StartTime = navigationTimestamp.UtcDateTime,
                    EndTime = navigationTimestamp.UtcDateTime
                },
                policy.DataTypes.Ids.BrowsingHistory.Value,
                cloudInstance,
                portal,
                isTest);

            return deleteRequest;
        }

        private static IEnumerable<DeleteRequest> ToUserPcfDeleteRequests(
            this TimelineCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            AppUsageCard appUsageCard;
            VoiceCard voiceCard;
            ContentConsumptionCard contentConsumptionCard;
            SearchCard searchCard;
            BrowseCard browseCard;
            LocationCard locationCard;
            if ((appUsageCard = card as AppUsageCard) != null)
            {
                yield return appUsageCard.ToUserPcfDeleteRequest(
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    policy,
                    cloudInstance,
                    portal,
                    isTest);
            }
            else if ((voiceCard = card as VoiceCard) != null)
            {
                yield return voiceCard.ToUserPcfDeleteRequest(
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    policy,
                    cloudInstance,
                    portal,
                    isTest);
            }
            else if ((contentConsumptionCard = card as ContentConsumptionCard) != null)
            {
                yield return contentConsumptionCard.ToUserPcfDeleteRequest(
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    policy,
                    cloudInstance,
                    portal,
                    isTest);
            }
            else if ((searchCard = card as SearchCard) != null)
            {
                foreach (DeleteRequest request in searchCard.ToUserPcfDeleteRequests(requestContext, requestGuid, correlationVector, timestamp, policy, cloudInstance, portal, isTest))
                    yield return request;
            }
            else if ((browseCard = card as BrowseCard) != null)
            {
                foreach (DeleteRequest request in browseCard.ToUserPcfDeleteRequests(requestContext, requestGuid, correlationVector, timestamp, policy, cloudInstance, portal, isTest))
                    yield return request;
            }
            else if ((locationCard = card as LocationCard) != null)
            {
                foreach (DeleteRequest request in locationCard.ToUserPcfDeleteRequests(
                    requestContext,
                    requestGuid,
                    correlationVector,
                    timestamp,
                    policy,
                    cloudInstance,
                    portal,
                    isTest))
                {
                    yield return request;
                }
            }
            else
            {
                throw new NotImplementedException($"Card type {card.GetType().FullName} not yet supported for single delete");
            }
        }

        private static IEnumerable<DeleteRequest> ToUserPcfDeleteRequests(
            this BrowseCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (card.Navigations == null)
                yield break;

            foreach (BrowseCard.Navigation navigation in card.Navigations)
            {
                foreach (DateTimeOffset navigationTimestamp in navigation.Timestamps)
                {
                    DeleteRequest deleteRequest = navigation.ToUserPcfDeleteRequest(
                        navigationTimestamp,
                        requestContext,
                        requestGuid,
                        correlationVector,
                        timestamp,
                        policy,
                        cloudInstance,
                        portal,
                        isTest);
                    deleteRequest.TimeRangePredicate = new TimeRangePredicate
                    {
                        StartTime = navigationTimestamp.UtcDateTime,
                        EndTime = navigationTimestamp.UtcDateTime
                    };

                    yield return deleteRequest;
                }
            }
        }

        private static IEnumerable<DeleteRequest> ToUserPcfDeleteRequests(
            this SearchCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            DeleteRequest baseRequest = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                null,
                null,
                policy.DataTypes.Ids.SearchRequestsAndQuery.Value,
                cloudInstance: cloudInstance,
                portal: portal,
                isTest: isTest);

            // Each impression gets a unique request id, so must have a separate request in storage.
            foreach (string impressionId in card.ImpressionIds)
            {
                var request = (DeleteRequest)baseRequest.ShallowCopyWithNewId();
                request.Predicate = new SearchRequestsAndQueryPredicate
                {
                    ImpressionGuid = impressionId
                };

                yield return request;
            }
        }

        private static IEnumerable<DeleteRequest> ToUserPcfDeleteRequests(
            this LocationCard card,
            IRequestContext requestContext,
            Guid requestGuid,
            string correlationVector,
            DateTimeOffset timestamp,
            Policy policy,
            string cloudInstance,
            string portal,
            bool isTest)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            // Handle the main cards' data as a single request
            DeleteRequest baseRequest = CreateDeleteRequest(
                CreateMsaSubjectFromContext(requestContext),
                requestContext,
                requestGuid,
                correlationVector,
                timestamp,
                null,
                null,
                new PreciseUserLocationPredicate
                {
                    Latitude = card.Location.Latitude,
                    Longitude = card.Location.Longitude
                },
                new TimeRangePredicate
                {
                    StartTime = card.Timestamp.UtcDateTime,
                    EndTime = card.Timestamp.UtcDateTime
                },
                policy.DataTypes.Ids.PreciseUserLocation.Value,
                cloudInstance,
                portal,
                isTest);

            yield return baseRequest;

            // Location cards can possibly contain additional impressions within the card. These contain lat/long and timestamp of the deaggregated data,
            // but they need to be stored as separate delete requests.
            if (card.AdditionalLocations == null || card.AdditionalLocations.Count == 0)
                yield break;

            // And also create requests for each additional location impression inside the card
            foreach (LocationCard.LocationImpression locationImpression in card.AdditionalLocations)
            {
                var deleteRequest = (DeleteRequest)baseRequest.ShallowCopyWithNewId();

                // start and end are the same value, matching the timestamp of the individual delete.
                // Note: we've had discussions on if this is right or not. This aligns most closely with how data is given to us from PDOS.
                deleteRequest.TimeRangePredicate = new TimeRangePredicate
                {
                    StartTime = locationImpression.Timestamp.UtcDateTime,
                    EndTime = locationImpression.Timestamp.UtcDateTime
                };

                // lat/long is specific to this
                deleteRequest.Predicate = new PreciseUserLocationPredicate
                {
                    Latitude = locationImpression.Latitude,
                    Longitude = locationImpression.Longitude
                };

                yield return deleteRequest;
            }
        }

        #region Subject converters

        /// <summary>
        ///     Creates an Aad subject or Aad subject2 from an aad identity and home tenant id
        /// </summary>
        public static PcfPrivacySubjects.AadSubject CreateAadSubjectFromIdentity(AadIdentity aadIdentity, Guid homeTenantId = default)
        {
            if (homeTenantId == default || homeTenantId == aadIdentity.TenantId)
            {
                return new PcfPrivacySubjects.AadSubject
                {
                    ObjectId = aadIdentity.TargetObjectId,
                    TenantId = aadIdentity.TenantId,
                    OrgIdPUID = aadIdentity.OrgIdPuid ?? 0
                };
            }
            else
            {
                return new PcfPrivacySubjects.AadSubject2
                {
                    ObjectId = aadIdentity.TargetObjectId,
                    TenantId = aadIdentity.TenantId,
                    OrgIdPUID = aadIdentity.OrgIdPuid ?? 0,
                    HomeTenantId = homeTenantId,
                    TenantIdType = PcfPrivacySubjects.TenantIdType.Resource
                };
            }
        }

        public static PcfPrivacySubjects.DeviceSubject CreateDeviceSubject(string globalDeviceId)
        {
            // TODO: Populate xboxConsoleId
            var deviceSubject = new PcfPrivacySubjects.DeviceSubject
            {
                GlobalDeviceId = long.Parse(RemoveGlobalFormat(globalDeviceId), NumberStyles.HexNumber)
            };
            return deviceSubject;
        }

        /// <summary>
        ///     Create an MSA subject from a request context.
        /// </summary>
        public static PcfPrivacySubjects.MsaSubject CreateMsaSubjectFromContext(IRequestContext requestContext)
        {
            var msaSubject = new PcfPrivacySubjects.MsaSubject();

            if (requestContext.TargetCid.HasValue)
                msaSubject.Cid = requestContext.TargetCid.Value;
            msaSubject.Puid = requestContext.TargetPuid;
            msaSubject.Anid = IdConverter.AnidFromPuid((ulong)requestContext.TargetPuid);
            msaSubject.Opid = IdConverter.OpidFromPuid((ulong)requestContext.TargetPuid);

            return msaSubject;
        }

        /// <summary>
        ///     Creates <see cref="PcfPrivacySubjects.DemographicSubject" /> from PXS <see cref="DemographicSubject" />.
        /// </summary>
        /// <param name="subject">Original privacy subject instance.</param>
        private static PcfPrivacySubjects.DemographicSubject CreateFromAltSubject(DemographicSubject subject)
        {
            return new PcfPrivacySubjects.DemographicSubject
            {
                Names = subject.Names,
                EmailAddresses = subject.Emails,
                PhoneNumbers = subject.Phones,
                Address = (subject.PostalAddress == null)
                    ? null
                    : new PcfPrivacySubjects.AddressQueryParams
                    {
                        StreetNumbers = subject.PostalAddress.StreetNumbers,
                        Streets = subject.PostalAddress.StreetNames,
                        UnitNumbers = subject.PostalAddress.UnitNumbers,
                        Cities = subject.PostalAddress.Cities,
                        States = subject.PostalAddress.Regions,
                        PostalCodes = subject.PostalAddress.PostalCodes
                    }
            };
        }

        /// <summary>
        ///     Creates <see cref="PcfPrivacySubjects.DemographicSubject" /> from PXS <see cref="PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject" />.
        /// </summary>
        /// <param name="subject">Original privacy subject instance.</param>
        private static PcfPrivacySubjects.DemographicSubject CreateFromAltSubject(PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject subject)
        {
            return new PcfPrivacySubjects.DemographicSubject
            {
                Names = subject.Names,
                EmailAddresses = subject.Emails,
                PhoneNumbers = subject.Phones,
                Address = (subject.PostalAddress == null)
                    ? null
                    : new PcfPrivacySubjects.AddressQueryParams
                    {
                        StreetNumbers = subject.PostalAddress.StreetNumbers,
                        Streets = subject.PostalAddress.StreetNames,
                        UnitNumbers = subject.PostalAddress.UnitNumbers,
                        Cities = subject.PostalAddress.Cities,
                        States = subject.PostalAddress.Regions,
                        PostalCodes = subject.PostalAddress.PostalCodes
                    }
            };
        }

        /// <summary>
        ///     Creates <see cref="PcfPrivacySubjects.DemographicSubject" /> from PXS <see cref="MicrosoftEmployeeSubject" />.
        /// </summary>
        /// <param name="subject">Original privacy subject instance.</param>
        private static PcfPrivacySubjects.IPrivacySubject CreateFromAltSubject(MicrosoftEmployeeSubject subject)
        {
            return new PcfPrivacySubjects.MicrosoftEmployee
            {
                EmployeeId = subject.EmployeeId,
                Emails = subject.Emails,
                StartDate = subject.EmploymentStart.UtcDateTime,
                EndDate = subject.EmploymentEnd.HasValue ? subject.EmploymentEnd.Value.UtcDateTime : DateTime.MaxValue
            };
        }

        /// <summary>
        ///     Creates <see cref="PcfPrivacySubjects.DemographicSubject" /> from PXS <see cref="PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject" />.
        /// </summary>
        /// <param name="subject">Original privacy subject instance.</param>
        private static PcfPrivacySubjects.IPrivacySubject CreateFromAltSubject(PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject subject)
        {
            return new PcfPrivacySubjects.MicrosoftEmployee
            {
                EmployeeId = subject.EmployeeId,
                Emails = subject.Emails,
                StartDate = subject.EmploymentStart.UtcDateTime,
                EndDate = subject.EmploymentEnd.HasValue ? subject.EmploymentEnd.Value.UtcDateTime : DateTime.MaxValue
            };
        }

        #endregion
    }
}
