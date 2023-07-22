namespace PCF.UnitTests
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Moq;

    using SubjectType = Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType;

    public static class TestDataBuilderMixins
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<MsaSubject> AnMsaSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<MsaSubject>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<AadSubject> AnAadSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<AadSubject>();
        }

        public static TestDataBuilder<AadSubject2> AnAadSubject2(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<AadSubject2>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<DeviceSubject> ADeviceSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<DeviceSubject>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<NonWindowsDeviceSubject> ANonWindowsDeviceSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<NonWindowsDeviceSubject>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<EdgeBrowserSubject> AEdgeBrowserSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<EdgeBrowserSubject>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<DemographicSubject> ADemographicSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<DemographicSubject>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<MicrosoftEmployee> AMicrosoftEmployeeSubject(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<MicrosoftEmployee>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<TimeRangePredicate> ATimeRangePredicate(this INeedDataBuilders that)
        {
            return new TimeRangePredicateBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<TPredicate> APredicate<TPredicate>(this INeedDataBuilders that) where TPredicate : IPrivacyPredicate
        {
            return new AutoFixtureTestDataBuilder<TPredicate>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static DeleteCommandBuilder ADeleteCommand(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null, DateTimeOffset? absoluteExpirationTime = null, string agentState = null)
        {
            var builder = new DeleteCommandBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            if (absoluteExpirationTime != null)
            {
                builder.WithValue(x => x.AbsoluteExpirationTime, absoluteExpirationTime.Value);
            }

            if (agentState != null)
            {
                builder.With(x => x.AgentState, agentState);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static ExportCommandBuilder AnExportCommand(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null, DateTimeOffset? absoluteExpirationTime = null, string agentState = null)
        {
            var builder = new ExportCommandBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            if (absoluteExpirationTime != null)
            {
                builder.WithValue(x => x.AbsoluteExpirationTime, absoluteExpirationTime.Value);
            }

            if (agentState != null)
            {
                builder.With(x => x.AgentState, agentState);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AccountCloseCommandBuilder AnAccountCloseCommand(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null, DateTimeOffset? absoluteExpirationTime = null, string agentState = null)
        {
            var builder = new AccountCloseCommandBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            if (absoluteExpirationTime != null)
            {
                builder.WithValue(x => x.AbsoluteExpirationTime, absoluteExpirationTime.Value);
            }

            if (agentState != null)
            {
                builder.With(x => x.AgentState, agentState);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AgeOutCommandBuilder AnAgeOutCommand(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null, SubjectType subjectType = SubjectType.Msa, DateTimeOffset? absoluteExpirationTime = null, string agentState = null)
        {
            var builder = new AgeOutCommandBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            switch (subjectType)
            {
                case SubjectType.Msa:
                    builder.With(x => x.Subject, builder.AnMsaSubject().Build());
                    break;
                case SubjectType.Aad:
                    builder.With(x => x.Subject, builder.AnAadSubject().Build());
                    break;
                case SubjectType.Device:
                    builder.With(x => x.Subject, builder.ADeviceSubject().Build());
                    break;
                case SubjectType.NonWindowsDevice:
                    builder.With(x => x.Subject, builder.ANonWindowsDeviceSubject().Build());
                    break;
                case SubjectType.EdgeBrowser:
                    builder.With(x => x.Subject, builder.AEdgeBrowserSubject().Build());
                    break;
                case SubjectType.Demographic:
                    builder.With(x => x.Subject, builder.ADemographicSubject().Build());
                    break;
                case SubjectType.MicrosoftEmployee:
                    builder.With(x => x.Subject, builder.AMicrosoftEmployeeSubject().Build());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(subjectType), subjectType, null);
            }

            if (absoluteExpirationTime != null)
            {
                builder.WithValue(x => x.AbsoluteExpirationTime, absoluteExpirationTime.Value);
            }

            if (agentState != null)
            {
                builder.With(x => x.AgentState, agentState);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static DeletePxsCommandBuilder ADeletePxsCommand(this INeedDataBuilders that)
        {
            return new DeletePxsCommandBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static ExportPxsCommandBuilder AnExportPxsCommand(this INeedDataBuilders that)
        {
            return new ExportPxsCommandBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AccountClosePxsCommandBuilder AnAccountClosePxsCommand(this INeedDataBuilders that)
        {
            return new AccountClosePxsCommandBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AgeOutPxsCommandBuilder AnAgeOutPxsCommand(this INeedDataBuilders that)
        {
            return new AgeOutPxsCommandBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AssetGroupInfo AnAssetGroupInfo(this INeedDataBuilders that)
        {
            return new AssetGroupInfo(that.AnAssetGroupInfoDocument(), true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AssetGroupInfoDocumentBuilder AnAssetGroupInfoDocument(this INeedDataBuilders that)
        {
            return new AssetGroupInfoDocumentBuilder();
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AssetGroupVariantInfoDocumentBuilder AnAssetGroupVariantInfoDocument(this INeedDataBuilders that)
        {
            return new AssetGroupVariantInfoDocumentBuilder();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static CommandId ACommandId(this INeedDataBuilders that)
        {
            return new CommandId(Guid.NewGuid());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static VariantId AVariantId(this INeedDataBuilders that)
        {
            return new VariantId(Guid.NewGuid());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AssetGroupId AnAssetGroupId(this INeedDataBuilders that)
        {
            return new AssetGroupId(Guid.NewGuid());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AgentId AnAgentId(this INeedDataBuilders that)
        {
            return new AgentId(Guid.NewGuid());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static LeaseReceiptBuilder ALeaseReceipt(
            this INeedDataBuilders that, 
            AgentId agentId = null, 
            AssetGroupId assetGroupId = null, 
            CommandId commandId = null, 
            Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType? subjectType = null,
            PrivacyCommandType commandType = PrivacyCommandType.Delete)
        {
            var builder = new LeaseReceiptBuilder();
            
            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }
            
            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }
            
            if (subjectType != null)
            {
                builder.WithValue(x => x.SubjectType, subjectType.Value);
            }

            if (commandType != PrivacyCommandType.Delete)
            {
                builder.WithValue(x => x.CommandType, commandType);
            }

            return builder;
        }

        internal static CoreCommandDocumentBuilder ACoreCommandDocument(this INeedDataBuilders that)
        {
            return new CoreCommandDocumentBuilder();
        }

        public static ColdStorageCommandRecordBuilder AColdStorageCommandRecord(this INeedDataBuilders that, CommandId commandId = null)
        {
            return new ColdStorageCommandRecordBuilder(commandId ?? that.ACommandId());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandStartedEvent> AStartedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandStartedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandSoftDeleteEvent> ASoftDeletedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandSoftDeleteEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandCompletedEvent> ACompletedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandCompletedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandSentToAgentEvent> ASentToAgentEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandSentToAgentEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandPendingEvent> APendingEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandPendingEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandFailedEvent> AFailedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandFailedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandUnexpectedEvent> AUnexpectedCommandEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandUnexpectedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandVerificationFailedEvent> AVerificationFailedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandVerificationFailedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandUnexpectedVerificationFailureEvent> AUnexpectedVerificationFailureEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandUnexpectedVerificationFailureEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandRawDataEvent> ARawDataEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandRawDataEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static TestDataBuilder<CommandDroppedEvent> ADroppedEvent(this INeedDataBuilders that, AgentId agentId = null, AssetGroupId assetGroupId = null, CommandId commandId = null)
        {
            var builder = new CommandDroppedEventBuilder();

            if (agentId != null)
            {
                builder.With(x => x.AgentId, agentId);
            }

            if (assetGroupId != null)
            {
                builder.With(x => x.AssetGroupId, assetGroupId);
            }

            if (commandId != null)
            {
                builder.With(x => x.CommandId, commandId);
            }

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static Mock<T> AMockOf<T>(this INeedDataBuilders that) where T : class
        {
            return new Mock<T>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "that")]
        public static AutoFixtureTestDataBuilder<T> AnInstanceOf<T>(this INeedDataBuilders that)
        {
            return new AutoFixtureTestDataBuilder<T>();
        }
    }
}