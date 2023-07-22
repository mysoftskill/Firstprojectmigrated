// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using System.ComponentModel;

    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json.Linq;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandIngesterTests : INeedDataBuilders
    {
        [Theory, Description("only drop suspended accounts when IngestionBlocked* flights are enabled")]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public void ShouldFlightsDropCommand(bool IsIngestionBlockedForAgentIdEnabled, bool IsIngestionBlockedForAssetGroupIdEnabled, bool expectedShouldDrop)
        {
            bool result;

            if (IsIngestionBlockedForAgentIdEnabled)
            {
                using (new FlightEnabled(FlightingNames.IngestionBlockedForAgentId))
                {
                    if (IsIngestionBlockedForAssetGroupIdEnabled)
                    {
                        using (new FlightEnabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            result = CommandIngester.ShouldFlightsDropCommand(this.AnAgentId(), this.AnAssetGroupId());
                        }
                    }
                    else
                    {
                        using (new FlightDisabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            result = CommandIngester.ShouldFlightsDropCommand(this.AnAgentId(), this.AnAssetGroupId());
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
                            result = CommandIngester.ShouldFlightsDropCommand(this.AnAgentId(), this.AnAssetGroupId());
                        }
                    }
                    else
                    {
                        using (new FlightDisabled(FlightingNames.IngestionBlockedForAssetGroupId))
                        {
                            result = CommandIngester.ShouldFlightsDropCommand(this.AnAgentId(), this.AnAssetGroupId());
                        }
                    }
                }
            }

            Assert.Equal(expectedShouldDrop, result);
        }
    }
}
