namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using System;

    /// <summary>
    /// Maps privacy command type to audit log action
    /// </summary>
    internal static class PrivacyCommandTypeToAuditLogActionMapper
    {
        internal static AuditLogCommandAction GetCommandCompletedAuditLogAction(CommandCompletedEvent completedEvent)
        {
            if (completedEvent.ForceCompleteReasonCode != null)
            {
                if (Config.Instance.Common.IsTestEnvironment)
                {
                    ProductionSafetyHelper.EnsureNotInProduction();
                }
                else if (completedEvent.CommandType != PrivacyCommandType.Export)
                {
                    // Force completions for Delete and Account should not occur and should not show up in audit.
                    throw new ArgumentException("Delete and Account close should not be force completed");
                }

                switch (completedEvent.ForceCompleteReasonCode.Value)
                {
                    case ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage when completedEvent.CommandType == PrivacyCommandType.Export:
                        return AuditLogCommandAction.ExportFailedByManualComplete;

                    case ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer when completedEvent.CommandType == PrivacyCommandType.Export:
                        return AuditLogCommandAction.ExportFailedByAutoComplete;

                    default:
                        // Keeping track of this, since this logic is moved from Receiver to Publisher side, and we don't expect this case to come up.
                        DualLogger.Instance.Warning(nameof(GetCommandCompletedAuditLogAction), 
                            $"Invalid {nameof(ForceCompleteReasonCode)} '{completedEvent.ForceCompleteReasonCode}' and {nameof(PrivacyCommandType)} '${completedEvent.CommandType}' combination.");
                        return AuditLogCommandAction.None;
                }
            }

            if (completedEvent.IgnoredByVariant)
            {
                return AuditLogCommandAction.IgnoredByVariant;
            }

            return GetCommandCompletedAuditLogAction(completedEvent.CommandType);
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for completed events.
        /// </summary>
        /// <param name="commandType"></param>
        /// <returns></returns>
        private static AuditLogCommandAction GetCommandCompletedAuditLogAction(PrivacyCommandType commandType)
        {
            switch (commandType)
            {
                case PrivacyCommandType.AccountClose:
                    return AuditLogCommandAction.HardDelete;

                case PrivacyCommandType.AgeOut:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.Delete:
                    return AuditLogCommandAction.HardDelete;

                case PrivacyCommandType.Export:
                    return AuditLogCommandAction.ExportComplete;

                default:
                    return AuditLogCommandAction.None;
            }
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for started events.
        /// </summary>
        internal static AuditLogCommandAction GetCommandStartedAuditLogAction(PrivacyCommandType commandType)
        {
            switch (commandType)
            {
                case PrivacyCommandType.AccountClose:
                    return AuditLogCommandAction.DeleteStart;

                case PrivacyCommandType.AgeOut:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.Delete:
                    return AuditLogCommandAction.DeleteStart;

                case PrivacyCommandType.Export:
                    return AuditLogCommandAction.ExportStart;

                default:
                    return AuditLogCommandAction.None;
            }
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for dropped events.
        /// </summary>
        internal static AuditLogCommandAction GetCommandDroppedAuditLogAction(PrivacyCommandType commandType)
        {
            switch (commandType)
            {
                case PrivacyCommandType.AccountClose:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.AgeOut:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.Delete:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.Export:
                    return AuditLogCommandAction.NotApplicable;

                default:
                    return AuditLogCommandAction.None;
            }
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for soft delete events.
        /// </summary>
        internal static AuditLogCommandAction GetCommandSoftDeleteAuditLogAction(PrivacyCommandType commandType)
        {
            switch (commandType)
            {
                case PrivacyCommandType.AccountClose:
                    return AuditLogCommandAction.SoftDelete;

                case PrivacyCommandType.AgeOut:
                    return AuditLogCommandAction.None;

                case PrivacyCommandType.Delete:
                    return AuditLogCommandAction.SoftDelete;

                case PrivacyCommandType.Export:
                    return AuditLogCommandAction.None;

                default:
                    return AuditLogCommandAction.None;
            }
        }
    }
}
