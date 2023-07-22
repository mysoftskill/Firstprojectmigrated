namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;

    using Xunit;

    /// <summary>
    /// Tests aspects of the "cold storage record" class, including the dirty flags.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class CommandHistoryRecordTests : INeedDataBuilders
    {
        [Fact]
        public void CommandHistoryChangeTrackedMap_TracksChanges()
        {
            CommandHistoryChangeTrackedMap<string, CommandHistoryAssetGroupStatusRecord> map = new CommandHistoryChangeTrackedMap<string, CommandHistoryAssetGroupStatusRecord>(this.ACommandId(), null);

            // Starts out as dirty.
            Assert.True(map.IsDirty);
            map.ClearDirty();
            Assert.False(map.IsDirty);

            var assetGroupRecord = new CommandHistoryAssetGroupStatusRecord(this.AnAgentId(), this.AnAssetGroupId())
            {
                Delinked = true
            };

            Assert.True(assetGroupRecord.IsDirty);
            assetGroupRecord.ClearDirty();
            Assert.False(assetGroupRecord.IsDirty);

            // Adding record to map marks the map as dirty.
            map["foobar"] = assetGroupRecord;
            Assert.True(map.IsDirty);
            map.ClearDirty();
            Assert.False(map.IsDirty);

            // Updating property on asset group record marks both it and map as dirty.
            // Setting map as not-dirty recursively clears dirty flag.
            assetGroupRecord.IngestionTime = DateTimeOffset.UtcNow;
            Assert.True(map.IsDirty);
            Assert.True(assetGroupRecord.IsDirty);
            map.ClearDirty();
            Assert.False(map.IsDirty);
            Assert.False(assetGroupRecord.IsDirty);

            map["foobar"] = null;
            Assert.True(map.IsDirty);
            map.ClearDirty();
        }

        [Fact]
        public void CommandHistoryAssetGroupRecord_TracksChanges()
        {
            this.ObjectTracksChanges(new CommandHistoryAssetGroupStatusRecord(this.AnAgentId(), this.AnAssetGroupId()));
        }

        [Fact]
        public void CommandHistoryExportDestinationRecord_TracksChanges()
        {
            var record = new CommandHistoryExportDestinationRecord(this.AnAgentId(), this.AnAssetGroupId(), new Uri("https://www.blah.com"), "/foo/bar/baz");
            Assert.False(record.IsDirty);

            int settablePropertyCount = typeof(CommandHistoryExportDestinationRecord).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(x => x.SetMethod != null)
                .Count();

            Assert.Equal(0, settablePropertyCount);
        }

        [Fact]
        public void CommandHistoryCoreRecord_TracksChanges()
        {
            this.ObjectTracksChanges(new CommandHistoryCoreRecord(this.ACommandId()));
        }

        [Fact]
        public void CommandIngestionAuditRecord_TracksChanges()
        {
            this.ObjectTracksChanges(new CommandIngestionAuditRecord());
        }

        private void ObjectTracksChanges(ICommandHistoryChangeTrackedObject changeTrackedObject)
        {
            Assert.True(changeTrackedObject.IsDirty);
            changeTrackedObject.ClearDirty();
            Assert.False(changeTrackedObject.IsDirty);

            Type type = changeTrackedObject.GetType();
            
            foreach (var propertyInfo in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (propertyInfo.GetMethod?.IsPublic == true && propertyInfo.SetMethod?.IsPublic == true)
                {
                    Type propertyType = propertyInfo.PropertyType;

                    object defaultValue = null;
                    if (propertyType.IsValueType)
                    {
                        defaultValue = Activator.CreateInstance(propertyType);
                    }

                    Assert.False(changeTrackedObject.IsDirty);
                    propertyInfo.SetValue(changeTrackedObject, defaultValue);
                    Assert.True(changeTrackedObject.IsDirty);

                    changeTrackedObject.ClearDirty();
                    Assert.False(changeTrackedObject.IsDirty);
                }
            }
        }
    }
}
