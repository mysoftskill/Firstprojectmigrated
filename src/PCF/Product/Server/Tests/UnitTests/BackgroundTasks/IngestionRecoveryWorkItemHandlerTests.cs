// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using System;
    using System.ComponentModel;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class IngestionRecoveryWorkItemHandlerTests : INeedDataBuilders
    {
        [Theory, Description("only recover commands when both ingestion and creation times are null")]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void ShouldRecoverRecord_NullRecordDates(bool IsIngestionTimeNull, bool IsCompletedTimeNull, bool expectedShouldRecover)
        {
            bool result;
            (AgentId AgentId, AssetGroupId AssetGroupId) key = (this.AnAgentId(), this.AnAssetGroupId());
            var record = new CommandHistoryAssetGroupStatusRecord(key.AgentId, key.AssetGroupId)
            {
                IngestionTime = IsIngestionTimeNull ? null : new DateTimeOffset?(DateTimeOffset.UtcNow),
                CompletedTime = IsCompletedTimeNull ? null : new DateTimeOffset?(DateTimeOffset.UtcNow)
            };

            using (new FlightDisabled(FlightingNames.IngestionBlockedForAgentId))
            {
                using (new FlightDisabled(FlightingNames.IngestionBlockedForAssetGroupId))
                {
                    result = IngestionRecoveryWorkItemHandler.ShouldRecoverRecord(key, record);
                }
            }

            Assert.Equal(expectedShouldRecover, result);
        }

        [Theory, Description("only recover commands when ingestion is allowed by flight")]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void ShouldRecoverRecord_IngestionFlightsEnabled(bool IsIngestionBlockedForAgentIdEnabled, bool IsIngestionBlockedForAssetGroupIdEnabled, bool expectedShouldRecover)
        {
            bool result;
            (AgentId AgentId, AssetGroupId AssetGroupId) key = (this.AnAgentId(), this.AnAssetGroupId());
            var record = new CommandHistoryAssetGroupStatusRecord(key.AgentId, key.AssetGroupId)
            {
                IngestionTime = null,
                CompletedTime = null
            };

            if (IsIngestionBlockedForAgentIdEnabled)
            {
                using (new FlightEnabled(FlightingNames.IngestionBlockedForAgentId))
                {
                    if (IsIngestionBlockedForAssetGroupIdEnabled)
                    {
                        using (new FlightEnabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            // False expected
                            result = IngestionRecoveryWorkItemHandler.ShouldRecoverRecord(key, record);
                        }
                    }
                    else
                    {
                        using (new FlightDisabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            // False expected
                            result = IngestionRecoveryWorkItemHandler.ShouldRecoverRecord(key, record);
                        }
                    }
                }
            }
            else
            {
                using (new FlightDisabled(FlightingNames.IngestionBlockedForAgentId))
                {
                    if (IsIngestionBlockedForAssetGroupIdEnabled)
                    {
                        using (new FlightEnabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            // False expected
                            result = IngestionRecoveryWorkItemHandler.ShouldRecoverRecord(key, record);
                        }
                    }
                    else
                    {
                        using (new FlightDisabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            // Both flights disabled. True expected.
                            result = IngestionRecoveryWorkItemHandler.ShouldRecoverRecord(key, record);
                        }
                    }
                }
            }

            Assert.Equal(expectedShouldRecover, result);
        }
    }
}
