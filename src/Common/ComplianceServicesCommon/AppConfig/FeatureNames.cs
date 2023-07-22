
namespace Microsoft.Azure.ComplianceServices.Common
{
    /// <summary>
    /// Defines all feature names that can be passed in IAppConfiguration.IsFeatureFlagEnabledAsync
    /// </summary>
    public class FeatureNames
    {

        public const string MultiTenantCollaboration = "MultiTenantCollaboration";

        // List of Ids that are blocked from calling PXS timeline API
        public const string TimelineApiBlocked = "TimelineApiBlocked";

        // Log extra token information for debugging
        public const string AuthenticationLogging = "PXS_AuthenticationLogging";

        public static class PAF
        {
            public const string PAF_AID_RunMockAidEventHubAsync_Enabled = "PAF_AID_RunMockAidEventHubAsync_Enabled";
            // Enable/Disable the connector for publishing to the actual ICM Portal
            public const string PAF_AID_ICMConnector_Enabled = "PAF_AID_ICMConnector_Enabled";
            // Enable/Disable storing blob trigger processing output data for manual verification
            public const string PAF_AID_ICMMockTestFiles_Enabled = "PAF_AID_ICMMockTestFiles_Enabled";
            // Enable/Disable the forwarding of eventhub events to azure queue
            public const string PAF_AID_EventsToQueue_Enabled = "PAF_AID_EventsToQueue_Enabled";
        }

        public static class PCF
        {
            // Publish uncompressed message to event hub
            public const string PublishUncompressedMessage = "PublishUncompressedMessage";

            // Whether rawcommand processor should be running in PCF worker
            public const string RawCommandProcessor = "RawCommandProcessor";

            // If hourly PCFConfig stream should be read instead of the daily one
            public const string ReadHourlyPCFConfigStream = "ReadHourlyPCFConfigStream";

            // if Replay should also include export commands
            public const string EnableExportCommandReplay = "EnableExportCommandReplay";
        }

        public static class PCFV2
        {
            // Write command ids to completed commands cosmos db
            public const string WriteCompletedCommands = "PCFV2.WriteCompletedCommands";

            // Read the export expectations cosmos db before completing commands 
            public const string ReadExportExpectations = "PCFV2.ReadExportExpectations";

            // Write force completed events to Event Hub with Null values for agent id and asset group id
            public const string WriteForceCompleteToEventHubWithNullValues = "PCFV2.WriteForceCompleteToEventHubWithNullValues";
        }

        public static class PDMS
        {
            // Support 1st Party and 3rd Party AppIds
            public const string DualAppIdSupport = "PDMS.DualAppIdSupport";
            public const string MiseAuthEnabled = "PDMS.MiseAuthEnabled";
        }

        public static class PXS
        {
            public const string AnaheimIdQueueWorker_Enabled = "PXS.AnaheimIdQueueWorker_Enabled";

            // Do not process account close for this user.
            public const string DropAccountCloseSignalForUser = "PXS.DropAccountCloseSignalForUser";

            public const string EnableEstsrForPopToken = "PXS.EnableEstsrForPopToken";

            public const string PersistStateAfterAppendToManifestFileSetQueue = "PXS.PersistStateAfterAppendToManifestFileSetQueue";

            // PXS Export worker, disabling enqueue of export files.
            public const string DisableEnqueueExportFilesForAgent = "PXS.DisableEnqueueExportFilesForAgent";

            // PXS Recurring Deletes API
            public const string RecurringDeleteAPIEnabled = "PXS.RecurringDeleteAPIEnabled";

            // PXS Recurring Deletes Worker Processing Job
            public const string RecurringDeleteWorkerEnabled = "PXS.RecurringDeleteWorkerEnabled";

            // PXS ScheduleDb Diagnostic Logging
            public const string ScheduleDbDiagnosticLoggingEnabled = "PXS.ScheduleDbDiagnosticLoggingEnabled";

            // PXS Device Delete traffic sending to PCF throttling
            public const string DeleteDeviceRequestEnabled = "PXS.DeleteDeviceRequestEnabled";

            // PXS Device Delete traffic sending to Anaheim throttling
            public const string AnaheimIdEventsPublishEnabled = "PXS.AnaheimIdEventsPublishEnabled";

            // PXS AnaheimId traffic sending to PCF throttling
            public const string AnaheimIdRequestToPcfEnabled = "PXS.AnaheimIdRequestToPcfEnabled";

            // PXS enable dead letter processing
            public const string DeadLetterReProcessingEnabled = "PXS.DeadLetterReProcessingEnabled";

            // Push to queue instead of explicit post to PCF
            public const string DeadLetterRePushingToQueueEnabled = "PXS.DeadLetterRePushingToQueueEnabled";

            // Allow uningested commands to be processed
            public const string ProcessNonIngestedExportCommandsEnabled = "PXS.ProcessNonIngestedExportCommandsEnabled";
        }
    }
}
