namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Response code for PDMS data parsing.
    /// </summary>
    internal enum PdmsInfoParserResult
    {
        /// <summary>
        /// The value was understood and applicable.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The value was unknown.
        /// </summary>
        Failure = 1,

        /// <summary>
        /// The value was understood and should be ignored.
        /// </summary>
        Ignore = 2,
    }

    /// <summary>
    /// Collection of helper methods for parsing items from the PDMS data corpus.
    /// </summary>
    internal static class PdmsInfoParser
    {
        /// <summary>
        /// Callback representing a TryParse method.
        /// </summary>
        private delegate PdmsInfoParserResult GenericTryParse<TIn, TOut>(TIn @in, out TOut @out);

        /// <summary>
        /// Attempts to parse the given string as a PdmsSubjectType.
        /// </summary>
        public static PdmsInfoParserResult TryParsePdmsSubjectType(string value, out PdmsSubjectType subjectType)
        {
            bool result = Enum.TryParse(value, true, out subjectType);

            if (!result || subjectType == PdmsSubjectType.Invalid)
            {
                subjectType = PdmsSubjectType.Invalid;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse subject type: {value}");

                return PdmsInfoParserResult.Failure;
            }

            return PdmsInfoParserResult.Success;
        }

        /// <summary>
        /// Parses the set of strings into well-defined Pdms Subjects.
        /// </summary>
        public static void ParseSubjects(IEnumerable<string> subjects, ICollection<PdmsSubjectType> results, bool enableTolerantParsing)
        {
            ParseCollection(subjects, results, enableTolerantParsing, TryParsePdmsSubjectType);
        }

        /// <summary>
        /// Attempts to parse the given value as a Data Type.
        /// </summary>
        public static PdmsInfoParserResult TryParseDataType(string value, out DataTypeId dataType)
        {
            bool result = Policies.Current.DataTypes.TryCreateId(value, out dataType);

            if (!result || dataType == null)
            {
                dataType = null;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse data type: {value}");

                return PdmsInfoParserResult.Failure;
            }

            return PdmsInfoParserResult.Success;
        }

        /// <summary>
        /// Attempts to parse the given value as a TenantId.
        /// </summary>
        public static PdmsInfoParserResult TryParseTenantId(string value, out TenantId tenantId)
        {
            bool result = TenantId.TryParse(value, out tenantId);

            if (!result || tenantId == null)
            {
                tenantId = null;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse tenantid: {value}");

                return PdmsInfoParserResult.Failure;
            }

            return PdmsInfoParserResult.Success;
        }

        /// <summary>
        /// Attempts to parse the given value as a AgentReadinessState
        /// </summary>
        public static PdmsInfoParserResult TryParseAgentReadinessState(string rawAgentReadiness, out AgentReadinessState readinessState)
        {
            bool result = Enum.TryParse(rawAgentReadiness, out AgentReadinessState state);
            
            if (string.IsNullOrWhiteSpace(rawAgentReadiness) || !result)
            {
                // default to ProdReady
                readinessState = AgentReadinessState.ProdReady;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse agent readiness state: {rawAgentReadiness}");

                return PdmsInfoParserResult.Failure;
            }

            readinessState = state;
            return PdmsInfoParserResult.Success;
        }

        /// <summary>
        /// Parses the set of strings into well-defined Data Types.
        /// </summary>
        public static void ParseDataTypes(IEnumerable<string> subjects, ICollection<DataTypeId> results, bool enableTolerantParsing)
        {
            ParseCollection(subjects, results, enableTolerantParsing, TryParseDataType);
        }

        /// <summary>
        /// Parses the set of strings into Tenant Ids.
        /// </summary>
        public static void ParseTenantIds(IEnumerable<string> tenantids, ICollection<TenantId> results, bool enableTolerantParsing)
        {
            ParseCollection(tenantids, results, enableTolerantParsing, TryParseTenantId);
        }

        /// <summary>
        /// Parses the set of strings into an AgentReadinessState enumerable.
        /// </summary>
        public static AgentReadinessState ParseAgentReadinessState(string rawAgentReadinessState, bool enableTolerantParsing)
        {
            PdmsInfoParserResult result = TryParseAgentReadinessState(rawAgentReadinessState, out AgentReadinessState output);

            if (result == PdmsInfoParserResult.Success)
            {
                return output;
            }

            if (result == PdmsInfoParserResult.Failure)
            {
                if (!enableTolerantParsing)
                {
                    throw new InvalidOperationException($"Failed to parse string '{rawAgentReadinessState}' as an AgentReadinessState");
                }

                return output;
            }

            throw new InvalidOperationException("Unexpected result: " + result.ToString());
        }

        /// <summary>
        /// Attempts to parse the given capability from the PDMS stream as a PCF command type.
        /// </summary>
        public static PdmsInfoParserResult TryParseCapability(string value, out PrivacyCommandType commandType)
        {
            var comparison = StringComparer.OrdinalIgnoreCase;

            if (comparison.Equals(value, "AgeOut"))
            {
                commandType = PrivacyCommandType.AgeOut;
                return PdmsInfoParserResult.Success;
            }

            if (comparison.Equals(value, "AccountClose"))
            {
                commandType = PrivacyCommandType.AccountClose;
                return PdmsInfoParserResult.Success;
            }
            
            if (comparison.Equals(value, "Delete"))
            {
                commandType = PrivacyCommandType.Delete;
                return PdmsInfoParserResult.Success;
            }

            if (comparison.Equals(value, "Export"))
            {
                commandType = PrivacyCommandType.Export;
                return PdmsInfoParserResult.Success;
            }

            // View is a valid schema element, but we don't care about it.
            if (comparison.Equals(value, "View"))
            {
                commandType = PrivacyCommandType.None;
                return PdmsInfoParserResult.Ignore;
            }

            DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse capability type: {value}");
            commandType = PrivacyCommandType.None;
            return PdmsInfoParserResult.Failure;
        }

        /// <summary>
        /// Parses the set of strings into well-defined capabilities.
        /// </summary>
        public static void ParseCapabilities(IEnumerable<string> subjects, ICollection<PrivacyCommandType> results, bool enableTolerantParsing)
        {
            ParseCollection(subjects, results, enableTolerantParsing, TryParseCapability);
        }

        /// <summary>
        /// Attempts to convert the given PDMS subject into the equivalent PCF subject.
        /// This method has the following characteristics:
        ///    1) For a known PDMS subject type that maps to a PCF subject type, the output is set to that value and returns Success.
        ///    2) For a known PDMS subject type that does not map to a PCF subject type, this output is set an invalid value and returns Ignore.
        ///    3) For an unknown PDMS subject type, the output is set to an invalid value and returns Failure.
        /// </summary>
        public static PdmsInfoParserResult TryParsePcfSubject(PdmsSubjectType pdmsSubject, out Common.SubjectType subjectType)
        {
            switch (pdmsSubject)
            {
                case PdmsSubjectType.AADUser:
                    subjectType = Common.SubjectType.Aad;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.AADUser2:
                    subjectType = Common.SubjectType.Aad2;
                    return PdmsInfoParserResult.Success;

                // Xbox is parsed into MSAUser for now.
                case PdmsSubjectType.MSAUser:
                case PdmsSubjectType.Xbox:
                    subjectType = Common.SubjectType.Msa;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.DemographicUser:
                    subjectType = Common.SubjectType.Demographic;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.MicrosoftEmployee:
                    subjectType = Common.SubjectType.MicrosoftEmployee;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.DeviceOther:
                case PdmsSubjectType.Windows10Device:
                    subjectType = Common.SubjectType.Device;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.NonWindowsDevice:
                    subjectType = Common.SubjectType.NonWindowsDevice;
                    return PdmsInfoParserResult.Success;

                case PdmsSubjectType.EdgeBrowser:
                    subjectType = Common.SubjectType.EdgeBrowser;
                    return PdmsInfoParserResult.Success;

                // Other is not significant to PCF.
                case PdmsSubjectType.Other:
                    subjectType = (Common.SubjectType)(-2);
                    return PdmsInfoParserResult.Ignore;

                default:
                    DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Unable to parse {pdmsSubject} as a PCF subject");
                    subjectType = (Common.SubjectType)(-1);
                    return PdmsInfoParserResult.Failure;
            }
        }

        /// <summary>
        /// Parses the set of strings into well-defined PCF subjects.
        /// </summary>
        public static void ParsePcfSubjects(
            IEnumerable<PdmsSubjectType> subjects, 
            ICollection<Common.SubjectType> results, 
            bool enableTolerantParsing)
        {
            ParseCollection(subjects, results, enableTolerantParsing, TryParsePcfSubject);
        }

        /// <summary>
        /// Tries to extract the subject type from the given IPrivacySubject.
        /// </summary>
        public static PdmsSubjectType GetPdmsSubjectFromPrivacySubject(IPrivacySubject subject)
        {
            switch (subject)
            {
                case AadSubject2 _:
                    return PdmsSubjectType.AADUser2;

                case AadSubject _:
                    return PdmsSubjectType.AADUser;

                case MsaSubject _:
                    return PdmsSubjectType.MSAUser;
                    
                case DemographicSubject _:
                    return PdmsSubjectType.DemographicUser;

                case MicrosoftEmployee _:
                    return PdmsSubjectType.MicrosoftEmployee;

                case DeviceSubject deviceSubject:
                    return deviceSubject.GlobalDeviceId != 0 ? 
                           PdmsSubjectType.Windows10Device : 
                           PdmsSubjectType.DeviceOther;

                case NonWindowsDeviceSubject _:
                    return PdmsSubjectType.NonWindowsDevice;
                    
                case EdgeBrowserSubject _:
                    return PdmsSubjectType.EdgeBrowser;

                default:
                    DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Unable to extract a PDMS subject from {subject?.GetType().Name}");
                    throw new ArgumentOutOfRangeException(nameof(subject));
            }
        }

        /// <summary>
        /// Attempts to parse the given value to a sovereign cloud id.
        /// </summary>
        public static PdmsInfoParserResult TryParseCloudInstanceId(string value, out CloudInstanceId cloudInstanceId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                cloudInstanceId = null;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), "Failed to parse sovereign cloud Id: null or empty.");

                return PdmsInfoParserResult.Failure;
            }

            bool result = Policies.Current.CloudInstances.TryCreateId(value, out cloudInstanceId);

            if (!result || cloudInstanceId == null)
            {
                cloudInstanceId = null;
                DualLogger.Instance.Warning(nameof(PdmsInfoParser), $"Failed to parse sovereign cloud Id: {value}");

                return PdmsInfoParserResult.Failure;
            }

            return PdmsInfoParserResult.Success;
        }

        /// <summary>
        /// Parses the set of strings into a collection of sovereign cloud ids.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the cloud instances collection is null or empty.</exception>
        public static void ParseSovereignCloudInstances(
            ICollection<string> cloudInstances,
            ICollection<CloudInstanceId> results,
            bool enableTolerantParsing)
        {
            if (cloudInstances == null || !cloudInstances.Any())
            {
                if (!FlightingUtilities.IsEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
                {
                    // Return 'All' if the flight is disabled(fallback to 'All' is enabled by default).
                    results.Add(Policies.Current.CloudInstances.Ids.All);
                    return;
                }

                if (!enableTolerantParsing)
                {
                    throw new InvalidOperationException("The sovereign cloud instances collection was null or empty.");
                }

                DualLogger.Instance.Warning(nameof(PdmsInfoParser), "The sovereign cloud instances collection was null or empty.");

                results.Clear();
                return;
            }

            ParseCollection(cloudInstances, results, enableTolerantParsing, TryParseCloudInstanceId);
        }

        /// <summary>
        /// Parses a string to a SovereignCloudInstanceId representing the deployment location.
        /// </summary>
        public static CloudInstanceId ParseDeploymentLocation(string rawDeploymentLocation, bool enableTolerantParsing)
        {
            PdmsInfoParserResult result = TryParseCloudInstanceId(rawDeploymentLocation, out CloudInstanceId output);

            if (result == PdmsInfoParserResult.Success)
            {
                return output;
            }

            if (result == PdmsInfoParserResult.Failure)
            {
                if (!FlightingUtilities.IsEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
                {
                    // Return Public if failure when tolerant parsing is enabled or flight is disabled(fallback to Public is enabled by default).
                    return Policies.Current.CloudInstances.Ids.Public;
                }

                if (!enableTolerantParsing)
                {
                    throw new InvalidOperationException($"Failed to parse string '{rawDeploymentLocation}' as a CloudInstanceId");
                }

                return null;
            }

            throw new InvalidOperationException("Unexpected result: " + result);
        }

        /// <summary>
        /// Generic helper to parse a collection.
        /// </summary>
        private static void ParseCollection<TIn, TOut>(
            IEnumerable<TIn> input,
            ICollection<TOut> output,
            bool enableTolerantParsing,
            GenericTryParse<TIn, TOut> parser)
        {
            foreach (TIn item in input)
            {
                PdmsInfoParserResult result = parser(item, out TOut outputItem);

                if (result == PdmsInfoParserResult.Success)
                {
                    output.Add(outputItem);
                }
                else if (result == PdmsInfoParserResult.Ignore)
                {
                    // Intentionally left empty.
                }
                else if (result == PdmsInfoParserResult.Failure)
                {
                    if (!enableTolerantParsing)
                    {
                        throw new InvalidOperationException($"Failed to parse {typeof(TIn).Name} '{item}' as a {typeof(TOut).Name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unexpected result: " + result.ToString());
                }
            }
        }
    }
}
