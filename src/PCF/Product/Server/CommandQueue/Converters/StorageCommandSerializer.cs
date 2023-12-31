namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Serializes commands for storage using the visitor pattern.
    /// </summary>
    internal class StorageCommandSerializer : PrivacyCommandVisitor<StoragePrivacyCommand>
    {
        protected override StoragePrivacyCommand Visit(DeleteCommand command)
        {
            var storageCommand = SerializeCommon(command);

            storageCommand.CommandInfo = JObject.FromObject(
                new StorageDeleteCommandInfo
                {
                    Predicate = command.Predicate,
                    PrivacyDataType = command.DataType.Value,
                    TimeRangePredicate = command.TimeRangePredicate
                });

            return storageCommand;
        }

        protected override StoragePrivacyCommand Visit(ExportCommand command)
        {
            var storageCommand = SerializeCommon(command);

            storageCommand.CommandInfo = JObject.FromObject(
                new StorageExportCommandInfo
                {
                    AzureBlobStorageUri = command.AzureBlobContainerTargetUri,
                    AzureBlobStoragePath = command.AzureBlobContainerPath,
                    PrivacyDataTypes = command.DataTypeIds.Select(x => x.Value).ToArray()
                });

            return storageCommand;
        }

        protected override StoragePrivacyCommand Visit(AccountCloseCommand command)
        {
            return SerializeCommon(command);
        }

        protected override StoragePrivacyCommand Visit(AgeOutCommand command)
        {
            var serialCmd = SerializeCommon(command);
            serialCmd.CommandInfo = JObject.FromObject(
                new StorageAgeOutCommandInfo
                {
                    UnixLastActiveTimeSeconds = command.LastActive?.ToNearestMsUtc().ToUnixTimeSeconds(),
                    IsSuspended = command.IsSuspended
                }); 

            return serialCmd;
        }

        private static StoragePrivacyCommand SerializeCommon(PrivacyCommand source)
        {
            return new StoragePrivacyCommand
            {
                AgentState = source.AgentState,
                RequestBatchId = source.RequestBatchId?.Value,
                Id = source.CommandId.Value,
                CommandType = source.CommandType,
                Subject = source.Subject,
                UnixNextVisibleTimeSeconds = source.NextVisibleTime.ToNearestMsUtc().ToUnixTimeSeconds(),
                CreatedTime = source.Timestamp.ToNearestMsUtc(),
                CorrelationVector = source.CorrelationVector,
                Verifier = source.Verifier,
                VerifierV3 = source.VerifierV3,
                AssetGroupQualifier = source.AssetGroupQualifier,
                CloudInstance = source.CloudInstance,
                ProcessorApplicable = source.ProcessorApplicable,
                ControllerApplicable = source.ControllerApplicable,
                CommandSource = source.CommandSource
            };
        }
    }
}
