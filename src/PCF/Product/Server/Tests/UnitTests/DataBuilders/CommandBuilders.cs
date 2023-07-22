namespace PCF.UnitTests
{
    using System;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;

    using Client = Microsoft.PrivacyServices.CommandFeed.Client;

    public abstract class CommandBuilder<T> : TestDataBuilder<T>, INeedDataBuilders where T : PrivacyCommand
    {
    }

    public class DeleteCommandBuilder : CommandBuilder<DeleteCommand>
    {
        public DeleteCommandBuilder WithDataType(DataTypeId dataType, IPrivacyPredicate predicate = null)
        {
            this.UnsafeWith(x => x.Predicate, null);
            this.UnsafeWith(x => x.DataType, dataType);
            this.UnsafeWith(x => x.Predicate, predicate);

            return this;
        }

        public DeleteCommandBuilder WithTimeRangePredicate(TimeRangePredicate timeRangePredicate)
        {
            this.UnsafeWith(x => x.TimeRangePredicate, timeRangePredicate);
            return this;
        }

        protected override DeleteCommand CreateNewObject()
        {
            return new DeleteCommand(
                new AgentId(Guid.NewGuid()),
                "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                string.Empty,
                string.Empty,
                new CommandId(Guid.NewGuid()),
                new RequestBatchId(Guid.NewGuid()),
                DateTimeOffset.UtcNow,
                new AutoFixtureTestDataBuilder<MsaSubject>().Build(),
                "agentState",
                new AssetGroupId(Guid.NewGuid()),
                "correlationVector",
                DateTimeOffset.UtcNow,
                Policies.Current.CloudInstances.Ids.Public.Value,
                string.Empty,
                true,
                true,
                this.APredicate<BrowsingHistoryPredicate>().Build(),
                this.APredicate<TimeRangePredicate>(),
                Policies.Current.DataTypes.Ids.BrowsingHistory,
                DateTimeOffset.UtcNow.AddDays(1),
                queueStorageType: QueueStorageType.AzureCosmosDb);
        }
    }

    public class ExportCommandBuilder : CommandBuilder<ExportCommand>
    {
        public ExportCommandBuilder WithDataTypes(params DataTypeId[] dataTypes)
        {
            this.UnsafeWith(e => e.DataTypeIds.ToArray(), dataTypes);
            return this;
        }

        protected override ExportCommand CreateNewObject()
        {
            return new ExportCommand(
                new AgentId(Guid.NewGuid()),
                "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                string.Empty,
                string.Empty,
                new CommandId(Guid.NewGuid()),
                new RequestBatchId(Guid.NewGuid()),
                DateTimeOffset.UtcNow,
                this.AnMsaSubject().Build(),
                "agentState",
                new AssetGroupId(Guid.NewGuid()),
                "correlationVector",
                DateTimeOffset.UtcNow,
                Policies.Current.CloudInstances.Ids.Public.Value,
                string.Empty,
                true,
                true,
                new[] { Policies.Current.DataTypes.Ids.BrowsingHistory, Policies.Current.DataTypes.Ids.ContentConsumption },
                DateTimeOffset.UtcNow.AddDays(1),
                queueStorageType: QueueStorageType.AzureCosmosDb);
        }
    }

    public class AccountCloseCommandBuilder : CommandBuilder<AccountCloseCommand>
    {
        protected override AccountCloseCommand CreateNewObject()
        {
            return new AccountCloseCommand(
                new AgentId(Guid.NewGuid()),
                "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                string.Empty,
                string.Empty,
                new CommandId(Guid.NewGuid()),
                new RequestBatchId(Guid.NewGuid()),
                DateTimeOffset.UtcNow,
                this.AnMsaSubject().Build(),
                "agentState",
                new AssetGroupId(Guid.NewGuid()),
                "correlationVector",
                DateTimeOffset.UtcNow,
                Policies.Current.CloudInstances.Ids.Public.Value,
                string.Empty,
                true,
                true,
                DateTimeOffset.UtcNow.AddDays(1),
                queueStorageType: QueueStorageType.AzureCosmosDb);
        }
    }

    public class AgeOutCommandBuilder : CommandBuilder<AgeOutCommand>
    {
        protected override AgeOutCommand CreateNewObject()
        {
            var now = DateTimeOffset.UtcNow;
            return new AgeOutCommand(
                new AgentId(Guid.NewGuid()),
                "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                string.Empty,
                string.Empty,
                new CommandId(Guid.NewGuid()),
                new RequestBatchId(Guid.NewGuid()),
                DateTimeOffset.UtcNow,
                this.AnMsaSubject().Build(),
                "agentState",
                new AssetGroupId(Guid.NewGuid()),
                "correlationVector",
                now,
                Policies.Current.CloudInstances.Ids.Public.Value,
                string.Empty,
                true,
                true,
                DateTimeOffset.UtcNow.AddDays(1),
                lastActive: now.AddMonths(-1 * 10 * 12),
                queueStorageType: QueueStorageType.AzureCosmosDb);
        }
    }
}
