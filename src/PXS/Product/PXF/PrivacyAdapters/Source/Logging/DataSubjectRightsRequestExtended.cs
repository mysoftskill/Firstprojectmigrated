// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Telemetry;

    /// <summary>
    ///     Provides extended information about the DSR request to simplify logging. This class inherits from an auto-generated class, <see cref="DataSubjectRightsRequest" />
    /// </summary>
    public class DataSubjectRightsRequestExtended
    {
        private const LogOption DefaultLogOption = LogOption.Realtime;

        internal readonly DataSubjectRightsRequest dataSubjectRightsRequest = new DataSubjectRightsRequest();

        internal readonly Action<Envelope> fillEnvelope;

        /// <summary>
        ///     Creates a new instance of <see cref="DataSubjectRightsRequestExtended" /> from the <see cref="PrivacyRequest" />
        /// </summary>
        /// <param name="request">The request</param>
        public DataSubjectRightsRequestExtended(PrivacyRequest request)
        {
            this.dataSubjectRightsRequest.ExtraData = new Dictionary<string, string>();

            if (request == null)
            {
                return;
            }

            this.dataSubjectRightsRequest.ProcessorApplicable = request.ProcessorApplicable;
            this.dataSubjectRightsRequest.ControllerApplicable = request.ControllerApplicable;
            this.dataSubjectRightsRequest.CloudInstance = request.CloudInstance ?? string.Empty;
            this.dataSubjectRightsRequest.Portal = request.Portal ?? string.Empty;
            this.dataSubjectRightsRequest.RequestId = request.RequestId.ToString();
            this.dataSubjectRightsRequest.RequestGuid = request.RequestGuid.ToString();
            this.dataSubjectRightsRequest.RequestTimestamp = request.Timestamp.ToString("u"); // "u" format is universal sortable format, ex: 2019-03-12 17:48:05Z
            this.dataSubjectRightsRequest.Requester = request.Requester ?? string.Empty;
            this.dataSubjectRightsRequest.IsTestRequest = request.IsTestRequest;

            this.dataSubjectRightsRequest.RequestType = request.RequestType.ToString();
            this.AddRequestTypeDetails(request);

            switch (request.Subject)
            {
                case AadSubject aadSubject:
                    this.dataSubjectRightsRequest.SubjectType = "AAD";
                    this.fillEnvelope = SllLoggingHelper.CreateUserInfo(UserIdType.AzureAdId, aadSubject.ObjectId.ToString()).FillEnvelope;
                    break;
                case DemographicSubject demographicSubject:
                case MicrosoftEmployee microsoftEmployeeSubject:

                    // This used to be called Demographic, but the correct/new/more-understood term is 'Alternate': https://aka.ms/NGPAltSubjectRequests
                    this.dataSubjectRightsRequest.SubjectType = "Alternate";
                    break;
                case DeviceSubject deviceSubject:
                    this.dataSubjectRightsRequest.SubjectType = "Device";
                    if (deviceSubject.GlobalDeviceId != default(long))
                    {
                        this.fillEnvelope = SllLoggingHelper.CreateDeviceInfo(deviceSubject.GlobalDeviceId).FillEnvelope;
                    }
                    else if (deviceSubject.XboxConsoleId != null)
                    {
                        this.fillEnvelope = SllLoggingHelper.CreateDeviceInfo(
                            DeviceIdType.XboxLiveHardwareId,
                            deviceSubject.XboxConsoleId.ToString()).FillEnvelope;
                    }

                    break;
                case MsaSubject msaSubject:
                    this.dataSubjectRightsRequest.SubjectType = "MSA";
                    this.fillEnvelope = SllLoggingHelper.CreateUserInfo(new MsaId(msaSubject.Puid, msaSubject.Cid)).FillEnvelope;

                    this.dataSubjectRightsRequest.ExtraData["HasXuid"] = !string.IsNullOrWhiteSpace(msaSubject.Xuid) && !string.Equals(default(int).ToString(), msaSubject.Xuid)
                        ? true.ToString().ToLowerInvariant()
                        : false.ToString().ToLowerInvariant();

                    break;
                case EdgeBrowserSubject edgeBrowserSubject:
                    this.dataSubjectRightsRequest.SubjectType = "EdgeBrowser";
                    this.dataSubjectRightsRequest.ExtraData["EdgeBrowserId"] = edgeBrowserSubject.EdgeBrowserId.ToString();
                    break;
                default:
                    this.dataSubjectRightsRequest.SubjectType = "Unknown";
                    break;
            }
        }

        /// <summary>
        ///     Log the request.
        /// </summary>
        public void Log()
        {
            try
            {
                if (this.fillEnvelope != null)
                {
                    this.dataSubjectRightsRequest.LogInformational(DefaultLogOption, this.fillEnvelope);
                }
                else
                {
                    this.dataSubjectRightsRequest.LogInformational(DefaultLogOption);
                }
            }
            catch (Exception e)
            {
                IfxTraceLogger.Instance.Error(nameof(DataSubjectRightsRequestExtended), e, "An exception occurred while trying to log this request.");
            }
        }

        /// <summary>
        ///     Add additional details based on request type.
        /// </summary>
        /// <param name="request"></param>
        private void AddRequestTypeDetails(PrivacyRequest request)
        {
            switch (request)
            {
                case DeleteRequest deleteRequest:

                    if (!string.IsNullOrWhiteSpace(deleteRequest.PrivacyDataType))
                    {
                        this.dataSubjectRightsRequest.PrivacyDataTypes = $"{deleteRequest.PrivacyDataType}";
                    }

                    break;

                case ExportRequest exportRequest:

                    if (exportRequest.PrivacyDataTypes != null)
                    {
                        this.dataSubjectRightsRequest.PrivacyDataTypes = string.Join(",", exportRequest.PrivacyDataTypes.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

                        if (string.IsNullOrWhiteSpace(this.dataSubjectRightsRequest.PrivacyDataTypes))
                        {
                            this.dataSubjectRightsRequest.PrivacyDataTypes = string.Empty;
                        }
                    }

                    break;

                case AgeOutRequest ageOutRequest:

                    if (ageOutRequest.LastActive != null)
                    {
                        this.dataSubjectRightsRequest.ExtraData["LastActive"] =
                            ageOutRequest.LastActive?.ToString("u"); // "u" format is universal sortable format, ex: 2019-03-12 17:48:05Z
                    }

                    this.dataSubjectRightsRequest.ExtraData["Suspended"] = ageOutRequest.IsSuspended.ToString();

                    break;
            }
        }
    }
}
