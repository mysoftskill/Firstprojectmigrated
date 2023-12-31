namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using AccountCloseCommand = Common.AccountCloseCommand;
    using AgeOutCommand = Common.AgeOutCommand;
    using DeleteCommand = Common.DeleteCommand;
    using ExportCommand = Common.ExportCommand;
    using PrivacyCommand = Common.PrivacyCommand;
    using JsonAccountCloseCommand = Client.AccountCloseCommand;
    using JsonAgeOutCommand = Client.AgeOutCommand;
    using JsonDeleteCommand = Client.DeleteCommand;
    using JsonExportCommand = Client.ExportCommand;
    using JsonPrivacyCommand = Client.PrivacyCommand;

    [Flags]
    internal enum CommandFeedSerializerOptions
    {
        None = 0,

        MultiTenantCollaborationSupported = 0x1,
    }

    /// <summary>
    /// Serializes commands into the PCF API.
    /// </summary>
    internal class CommandFeedSerializer : PrivacyCommandVisitor<JsonPrivacyCommand>
    {
        private CommandFeedSerializerOptions serializerOptions;

        public CommandFeedSerializer(CommandFeedSerializerOptions serializerOptions)
        {
            this.serializerOptions = serializerOptions;
        }

        protected override JsonPrivacyCommand Visit(DeleteCommand command)
        {
            JsonDeleteCommand jsonCommand = new JsonDeleteCommand
            {
                TimeRangePredicate = command.TimeRangePredicate,
                DataTypePredicate = command.Predicate,
                PrivacyDataType = command.DataType
            };

            SerializeBase(jsonCommand, command, serializerOptions);
            return jsonCommand;
        }

        protected override JsonPrivacyCommand Visit(ExportCommand command)
        {
            JsonExportCommand jsonCommand = new JsonExportCommand
            {
                AzureBlobContainerTargetUri = command.AzureBlobContainerTargetUri,
                AzureBlobContainerPath = command.AzureBlobContainerPath,
                PrivacyDataTypes = command.DataTypeIds.ToArray(),
            };

            SerializeBase(jsonCommand, command, serializerOptions);
            return jsonCommand;
        }

        protected override JsonPrivacyCommand Visit(AccountCloseCommand command)
        {
            JsonAccountCloseCommand jsonCommand = new JsonAccountCloseCommand();
            SerializeBase(jsonCommand, command, serializerOptions);
            return jsonCommand;
        }

        protected override JsonPrivacyCommand Visit(AgeOutCommand command)
        {
            JsonAgeOutCommand jsonCommand = new JsonAgeOutCommand();
            SerializeBase(jsonCommand, command, serializerOptions);
            jsonCommand.LastActive = command.LastActive;
            jsonCommand.IsSuspended = command.IsSuspended ?? false;

            return jsonCommand;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void SerializeBase(JsonPrivacyCommand target, PrivacyCommand source, CommandFeedSerializerOptions serializerOptions)
        {
            target.AssetGroupId = source.AssetGroupId.Value;
            target.AssetGroupQualifier = source.AssetGroupQualifier;
            target.ApplicableVariants = source.ApplicableVariants?.Select(
                variant => new Variant
                {
                    VariantId = variant.VariantId.Value,
                    VariantName = variant.VariantName,
                    VariantDescription = variant.VariantDescription,
                    AssetQualifier = variant.AssetGroupQualifier,
                    DataTypeIds = variant.ApplicableDataTypeIds
                }).ToList();
            target.CommandId = source.CommandId.Value;
            target.CorrelationVector = source.CorrelationVector;
            target.ApproximateLeaseExpiration = source.NextVisibleTime.ToNearestMsUtc();
            target.LeaseReceipt = source.LeaseReceipt?.Serialize();
            target.Timestamp = source.Timestamp.ToNearestMsUtc();
            target.AgentState = source.AgentState;
            target.RequestBatchId = source.RequestBatchId.Value;
            target.CloudInstance = source.CloudInstance;

            if (source.Subject is AadSubject2 aadSubject2)
            {
                if ((serializerOptions & CommandFeedSerializerOptions.MultiTenantCollaborationSupported) != 0)
                {
                    // Multi-tenant collaboration opt-in agent, return AAdSubject2 and v3 verifier
                    target.Subject = source.Subject;

                    // Workaround for older commands that don't have V3 verifier
                    target.Verifier = !string.IsNullOrEmpty(source.VerifierV3) ? source.VerifierV3 : source.Verifier;
                }
                else
                {
                    // non-Cross-Tenant agent, return AAdSubject and v2 verifier
                    target.Subject = new AadSubject
                    {
                        TenantId = aadSubject2.TenantId,
                        ObjectId = aadSubject2.ObjectId,
                        OrgIdPUID = aadSubject2.OrgIdPUID,
                    };
                    target.Verifier = source.Verifier;
                }
            }
            else
            {
                target.Subject = source.Subject;
                target.Verifier = source.Verifier;
            }

            // Add sensible defaults for processor and controller applicable based on subject type.
            // These are only observed when the command does not come with a value set.
            target.ControllerApplicable = false;
            target.ProcessorApplicable = false;

            if (source.CommandType == PrivacyCommandType.AccountClose)
            {
                target.ControllerApplicable = source.ControllerApplicable ?? true;
                target.ProcessorApplicable = source.ProcessorApplicable ?? true; ;
            }

            var subjectType = source.Subject.GetSubjectType();
            if (source.ControllerApplicable != null)
            {
                target.ControllerApplicable = source.ControllerApplicable.Value;
            }
            else if (
                subjectType == SubjectType.Msa 
                || subjectType == SubjectType.Device 
                || subjectType == SubjectType.Demographic
                || subjectType == SubjectType.MicrosoftEmployee
                || subjectType == SubjectType.NonWindowsDevice
                || subjectType == SubjectType.EdgeBrowser)
            {
                target.ControllerApplicable = true;
            }

            if (source.ProcessorApplicable != null)
            {
                target.ProcessorApplicable = source.ProcessorApplicable.Value;
            }
            else if (subjectType == SubjectType.Aad || subjectType == SubjectType.Aad2 || subjectType == SubjectType.Demographic || subjectType == SubjectType.MicrosoftEmployee)
            {
                target.ProcessorApplicable = true;
            }
        }
    }
}
