// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net;
    using System.Runtime.InteropServices.WindowsRuntime;

    /// <summary>
    /// Library helpers.
    /// </summary>
    static public class NonWindowsDeviceDeleteHelpers
    {

        /// <summary>
        /// NonWindowsDeviceDelete feature flag.
        /// </summary>
        /// <returns></returns>
        public static bool IsNonWindowsDeviceDeleteEnabled()
        {
            bool disabled = FlightingUtilities.IsEnabled(FlightingNames.NonWindowsDeviceDeleteDisabled);

            if (disabled)
            {
                DualLogger.Instance.Information(nameof(IsNonWindowsDeviceDeleteEnabled), "NonWindowsDevice Deletes Pipeline: [DISABLED]");
            }
            else
            {
                DualLogger.Instance.Information(nameof(IsNonWindowsDeviceDeleteEnabled), "NonWindowsDevice Deletes Pipeline: [ENABLED]");
            }
            
            return !disabled;
        }

        /// <summary>
        /// Supported DataTypeIds by Device Delete signals.
        /// </summary>
        public static readonly DataTypeId[] SupportedDataTypeIds =
        {
                Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration,
                Policies.Current.DataTypes.Ids.ProductAndServiceUsage,
                Policies.Current.DataTypes.Ids.ProductAndServicePerformance,
                Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory,
                Policies.Current.DataTypes.Ids.BrowsingHistory,
                Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance
        };

        /// <summary>
        /// PXS Delete Request Requester.
        /// </summary>
        public static readonly string NonWindowsDeviceDeleteRequester = "1DS";

        /// <summary>
        /// PXS Delete Request Requester Portal.
        /// </summary>
        public static readonly string NonWindowsDeviceDeletePortal = "1DSNonWindowsDeleteSignal";

        /// <summary>
        /// Get device id value from json event.
        /// </summary>
        /// <param name="json">AEF EventHub json event.</param>
        /// <returns></returns>
        public static string GetDeviceIdFromJsonEvent(string json)
        {
            JObject parsedJson = JObject.Parse(json);

            // According to Common Schema that is the device id location:
            return (string)parsedJson["ext"]["device"]["localId"];
        }

        /// <summary>
        /// Validate json event.
        /// </summary>
        /// <param name="json">Json event.</param>
        /// <param name="reason">Validation explanation string.</param>
        /// <returns></returns>
        public static bool IsJsonEventValid(string json, out string reason)
        {
            reason = "OK";
            try
            {
                string deviceId = GetDeviceIdFromJsonEvent(json);
                if (string.IsNullOrEmpty(deviceId))
                {
                    reason = $"DeviceId cannot be null or empty.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                reason = $"Fail to get device id from event. Exception: {ex}";
                return false; 
            }
        }

        /// <summary>
        /// Create DeleteRequest from json delete event.
        /// </summary>
        /// <param name="json">Json event</param>
        /// <param name="requestGuid">Request GUID</param>
        /// <param name="dataTypeId">Privacy DataTypeId</param>
        /// <returns></returns>
        public static DeleteRequest CreateDeleteRequestFromJson(string json, Guid requestGuid, DataTypeId dataTypeId)
        {
            JObject parsedJson = JObject.Parse(json);
            DeleteRequest deleteRequest = null;

            Logger.InstrumentSynchronous(
                new IncomingEvent(SourceLocation.Here()),
                ev =>
                {
                    // See CommonSchema spec for parsing details: https://github.com/microsoft/common-schema/blob/master/v3.0/README.md
                    deleteRequest = new DeleteRequest()
                    {
                        // currently ext\user section is empty in delete event
                        AuthorizationId = (string)(parsedJson["ext"]["user"]["id"] ?? (string)parsedJson["ext"]["user"]["localId"]),
                        RequestId = Guid.NewGuid(),
                        RequestType = RequestType.Delete,
                        PrivacyDataType = dataTypeId.Value,
                        Predicate = CreatePrivacyPredicate(dataTypeId),
                        Timestamp = DateTimeOffset.UtcNow,
                        RequestGuid = requestGuid,
                        CorrelationVector = (string)parsedJson["cV"],
                        TimeRangePredicate = new TimeRangePredicate { EndTime = (DateTimeOffset)parsedJson["time"] },
                        VerificationToken = "",
                        Requester = NonWindowsDeviceDeleteRequester,
                        Portal = NonWindowsDeviceDeletePortal,
                        Subject = new NonWindowsDeviceSubject
                        {
                            MacOsPlatformDeviceId = ParseDeviceIdAsGuid(GetDeviceIdFromJsonEvent(json))
                        }
                    };

                    // Delete Request Guid
                    ev["RequestGuid"] = deleteRequest.RequestGuid.ToString();
                    // Delete Request Id (PCF CommandId)
                    ev["RequestId"] = deleteRequest.RequestId.ToString();
                    // Common Schema NonWindows Device Delete Request Correlation Vector
                    ev["Request_cV"] = deleteRequest.CorrelationVector;

                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });

            return deleteRequest;
        }

        internal static Guid ParseDeviceIdAsGuid(string deviceId)
        {
            string[] parts = deviceId.Split(new[] { ':' }, 2);
            if (!Guid.TryParse(parts[1], out var uuid))
            {
                throw new ArgumentException($"Cannot parse device uuid from {deviceId}. Expected format: u:uuid");
            }

            return uuid;
        }

        internal static IPrivacyPredicate CreatePrivacyPredicate(DataTypeId dataTypeId)
        {
            IPrivacyPredicate predicate;

            if (dataTypeId == Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration)
            {
                predicate = new DeviceConnectivityAndConfigurationPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.ProductAndServiceUsage)
            {
                predicate = new ProductAndServiceUsagePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.ProductAndServicePerformance)
            {
                predicate = new ProductAndServicePerformancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory)
            {
                predicate = new SoftwareSetupAndInventoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.BrowsingHistory)
            {
                predicate = new BrowsingHistoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance)
            {
                predicate = new InkingTypingAndSpeechUtterancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Unsupported privacy data type: {dataTypeId.Value}");
            }

            return predicate;
        }
    }
}
