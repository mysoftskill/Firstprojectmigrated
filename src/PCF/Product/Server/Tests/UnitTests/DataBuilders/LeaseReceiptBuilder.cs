namespace PCF.UnitTests
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public class LeaseReceiptBuilder : TestDataBuilder<LeaseReceipt>, INeedDataBuilders
    {
        protected override LeaseReceipt CreateNewObject()
        {
            return new LeaseReceipt(
                "moniker",
                this.ACommandId(),
                "etag",
                this.AnAssetGroupId(),
                this.AnAgentId(),
                SubjectType.Msa,
                DateTimeOffset.UtcNow.AddMinutes(1),
                "fakequalifier",
                PrivacyCommandType.Delete,
                string.Empty,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                QueueStorageType.AzureCosmosDb);
        }
    }
}
