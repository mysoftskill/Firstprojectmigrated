namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json.Linq;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    public class PxsCommandParser : ICommandVisitor<JObject, (PrivacyCommand pcfCommand, PXSV1.PrivacyRequest pxsCommand)>
    {
        private const int VerifierExpirationDays = 60;

        private readonly AgentId agentId;
        private readonly AssetGroupId assetGroupId;
        private readonly string assetGroupQualifier;
        private readonly QueueStorageType queueStorageType;

        public PxsCommandParser(AgentId agentId, AssetGroupId assetGroupId, string assetGroupQualifier, QueueStorageType queueStorageType)
        {
            this.agentId = agentId;
            this.assetGroupId = assetGroupId;
            this.assetGroupQualifier = assetGroupQualifier;
            this.queueStorageType = queueStorageType;
        }

        /// <summary>
        /// Returns a static dummy parser instance. Commands parsed from the dummy instance contain invalid AgentId, AssetGroupId, AssetGroupQualifier, and QueueStorageType.
        /// </summary>
        public static PxsCommandParser DummyParser { get; } = new PxsCommandParser(new AgentId(Guid.Empty), new AssetGroupId(Guid.Empty), string.Empty, QueueStorageType.Undefined);

        public PrivacyCommandType Classify(JObject rawPxsCommand)
        {
            var request = rawPxsCommand.ToObject<PXSV1.PrivacyRequest>();

            switch (request?.RequestType)
            {
                case PXSV1.RequestType.Delete:
                    return PrivacyCommandType.Delete;

                case PXSV1.RequestType.Export:
                    return PrivacyCommandType.Export;

                case PXSV1.RequestType.AccountClose:
                    return PrivacyCommandType.AccountClose;

                case PXSV1.RequestType.AgeOut:
                    return PrivacyCommandType.AgeOut;

                default:
                    return PrivacyCommandType.None;
            }
        }

        public (PrivacyCommand pcfCommand, PXSV1.PrivacyRequest pxsCommand) VisitAccountClose(JObject accountCloseCommand)
        {
            var pxsCommand = accountCloseCommand.ToObject<PXSV1.AccountCloseRequest>();

            IPrivacySubject subject = pxsCommand.Subject;
            if (pxsCommand.Subject is AadSubject2 aadSubject2)
            {
                if (aadSubject2.HomeTenantId == default || aadSubject2.HomeTenantId == aadSubject2.TenantId)
                {
                    subject = new AadSubject
                    {
                        TenantId = aadSubject2.TenantId,
                        OrgIdPUID = aadSubject2.OrgIdPUID,
                        ObjectId = aadSubject2.ObjectId,
                    };
                }
            } 

            var pcfCommand = new AccountCloseCommand(
                agentId: this.agentId,
                assetGroupQualifier: this.assetGroupQualifier,
                verifier: pxsCommand.VerificationToken,
                verifierV3: pxsCommand.VerificationTokenV3,
                commandId: new CommandId(pxsCommand.RequestId),
                batchId: new RequestBatchId(pxsCommand.RequestGuid),
                nextVisibleTime: DateTimeOffset.UtcNow,
                subject: subject,
                clientCommandState: string.Empty,
                assetGroupId: this.assetGroupId,
                correlationVector: pxsCommand.CorrelationVector,
                timestamp: pxsCommand.Timestamp,
                cloudInstance: pxsCommand.CloudInstance,
                commandSource: pxsCommand.Portal,
                processorApplicable: pxsCommand.ProcessorApplicable,
                controllerApplicable: pxsCommand.ControllerApplicable,
                absoluteExpirationTime: GetAbsoluteExpirationTime(pxsCommand.Timestamp),
                queueStorageType: this.queueStorageType)
            {
                IsSyntheticTestCommand = pxsCommand.IsSyntheticRequest,
            };

            return (pcfCommand, pxsCommand);
        }

        public (PrivacyCommand pcfCommand, PXSV1.PrivacyRequest pxsCommand) VisitAgeOut(JObject ageOutCommand)
        {
            var pxsCommand = ageOutCommand.ToObject<PXSV1.AgeOutRequest>();

            var pcfCommand = new AgeOutCommand(
                agentId: this.agentId,
                assetGroupQualifier: this.assetGroupQualifier,
                verifier: pxsCommand.VerificationToken,
                verifierV3: pxsCommand.VerificationTokenV3,
                commandId: new CommandId(pxsCommand.RequestId),
                batchId: new RequestBatchId(pxsCommand.RequestGuid),
                nextVisibleTime: DateTimeOffset.UtcNow,
                subject: pxsCommand.Subject,
                clientCommandState: string.Empty,
                assetGroupId: this.assetGroupId,
                correlationVector: pxsCommand.CorrelationVector,
                timestamp: pxsCommand.Timestamp,
                cloudInstance: pxsCommand.CloudInstance,
                commandSource: pxsCommand.Portal,
                processorApplicable: pxsCommand.ProcessorApplicable,
                controllerApplicable: pxsCommand.ControllerApplicable,
                absoluteExpirationTime: GetAbsoluteExpirationTime(pxsCommand.Timestamp),
                lastActive: pxsCommand.LastActive,
                queueStorageType: this.queueStorageType,
                isSuspended: pxsCommand.IsSuspended)
            {
                IsSyntheticTestCommand = pxsCommand.IsSyntheticRequest,
            };

            return (pcfCommand, pxsCommand);
        }

        public (PrivacyCommand pcfCommand, PXSV1.PrivacyRequest pxsCommand) VisitDelete(JObject deleteCommand)
        {
            var pxsCommand = deleteCommand.ToObject<PXSV1.DeleteRequest>();

            var commandId = new CommandId(pxsCommand.RequestId);
            if (pxsCommand.TimeRangePredicate == null)
            {
                Logger.Instance?.NullPxsTimeRangePredicate(commandId);

                pxsCommand.TimeRangePredicate = new Contracts.Predicates.TimeRangePredicate
                {
                    StartTime = DateTimeOffset.MinValue,
                    EndTime = DateTimeOffset.UtcNow,
                };
            }

            var pcfCommand = new DeleteCommand(
                agentId: this.agentId,
                assetGroupQualifier: this.assetGroupQualifier,
                verifier: pxsCommand.VerificationToken,
                verifierV3: pxsCommand.VerificationTokenV3,
                commandId: commandId,
                batchId: new RequestBatchId(pxsCommand.RequestGuid),
                nextVisibleTime: DateTimeOffset.UtcNow,
                subject: pxsCommand.Subject,
                clientCommandState: string.Empty,
                assetGroupId: this.assetGroupId,
                correlationVector: pxsCommand.CorrelationVector,
                timestamp: pxsCommand.Timestamp,
                cloudInstance: pxsCommand.CloudInstance,
                commandSource: pxsCommand.Portal,
                processorApplicable: pxsCommand.ProcessorApplicable,
                controllerApplicable: pxsCommand.ControllerApplicable,
                dataTypePredicate: pxsCommand.Predicate,
                timePredicate: pxsCommand.TimeRangePredicate,
                dataType: Policies.Current.DataTypes.CreateId(pxsCommand.PrivacyDataType),
                absoluteExpirationTime: GetAbsoluteExpirationTime(pxsCommand.Timestamp),
                queueStorageType: this.queueStorageType)
            {
                IsSyntheticTestCommand = pxsCommand.IsSyntheticRequest,
            };

            return (pcfCommand, pxsCommand);
        }

        public (PrivacyCommand pcfCommand, PXSV1.PrivacyRequest pxsCommand) VisitExport(JObject exportCommand)
        {
            var pxsCommand = exportCommand.ToObject<PXSV1.ExportRequest>();

            var pcfCommand = new ExportCommand(
                agentId: this.agentId,
                assetGroupQualifier: this.assetGroupQualifier,
                verifier: pxsCommand.VerificationToken,
                verifierV3: pxsCommand.VerificationTokenV3,
                commandId: new CommandId(pxsCommand.RequestId),
                batchId: new RequestBatchId(pxsCommand.RequestGuid),
                nextVisibleTime: DateTimeOffset.UtcNow.AddMinutes(Config.Instance.Common.ExportInitialVisibilityDelayMinutes),
                subject: pxsCommand.Subject,
                clientCommandState: string.Empty,
                assetGroupId: this.assetGroupId,
                correlationVector: pxsCommand.CorrelationVector,
                timestamp: pxsCommand.Timestamp,
                cloudInstance: pxsCommand.CloudInstance,
                commandSource: pxsCommand.Portal,
                processorApplicable: pxsCommand.ProcessorApplicable,
                controllerApplicable: pxsCommand.ControllerApplicable,
                dataTypes: pxsCommand.PrivacyDataTypes.Select(Policies.Current.DataTypes.CreateId),
                absoluteExpirationTime: GetAbsoluteExpirationTime(pxsCommand.Timestamp),
                queueStorageType: this.queueStorageType)
            {
                IsSyntheticTestCommand = pxsCommand.IsSyntheticRequest,
            };

            return (pcfCommand, pxsCommand);
        }

        private static DateTimeOffset GetAbsoluteExpirationTime(DateTimeOffset createTime)
        {
            var verifierExpirationTime = createTime.AddDays(VerifierExpirationDays);
            var defaultExpirationTime = DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays);

            // Compare above two calculated timestamp and use the earlier one as the absolutExpirationTime for later TTL calculation
            // DateTimeOffset.Compare() return <0 means first one is earlier, >0 means second one is earlier, 0 means same
            return (DateTimeOffset.Compare(verifierExpirationTime, defaultExpirationTime) < 0) ? verifierExpirationTime : defaultExpirationTime;
        }
    }
}
