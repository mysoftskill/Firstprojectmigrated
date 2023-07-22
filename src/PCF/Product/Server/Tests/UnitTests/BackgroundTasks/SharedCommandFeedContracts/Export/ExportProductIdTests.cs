// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests.BackgroundTasks.SharedCommandFeedContracts.Export
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Client;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ExportProductIdTests
    {
        [Fact]
        public void ContainsExpectedValues()
        {
            foreach (KeyValuePair<int, ExpectedExportProductId> expected in ExpectedExportProductId.ProductIds)
            {
                Assert.True(ExportProductId.ProductIds.TryGetValue(expected.Key, out ExportProductId actual), expected.Value.ToString());
                Assert.Equal(expected.Value.Id, actual.Id);
                Assert.Equal(expected.Value.Path, actual.Path);
            }
        }

        [Fact]
        public void DoesNotContainAdditionalValues()
        {
            foreach (KeyValuePair<int, ExportProductId> actual in ExportProductId.ProductIds)
            {
                Assert.True(ExpectedExportProductId.ProductIds.TryGetValue(actual.Key, out ExpectedExportProductId expected), actual.Value.ToString());
                Assert.Equal(expected.Id, actual.Value.Id);
                Assert.Equal(expected.Path, actual.Value.Path);
            }
        }
    }
}
