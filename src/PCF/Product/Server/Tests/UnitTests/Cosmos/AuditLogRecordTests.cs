namespace PCF.UnitTests.Cosmos
{
    using System;
    using System.Globalization;
    using System.Web;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.AuditLog;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AuditLogRecordTests
    {
        [Fact]
        public void RecordToStringTest()
        {
            var commandId = new CommandId(Guid.NewGuid());
            var agentId = new AgentId(Guid.NewGuid());
            var assetGroupId = new AssetGroupId(Guid.NewGuid());
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string assetGroupQualifier = "AssetType=AzureDocumentDB;AccountName=abcde";
            var action = AuditLogCommandAction.HardDelete;
            int rowCount = 10;
            string exceptions = "excetions1;exeption2;exception3";
            var commandType = PrivacyCommandType.Delete;

            string variant1 = Guid.NewGuid().ToString("d");
            string variant2 = Guid.NewGuid().ToString("d");
            string[] variantsApplied = { variant1, variant2 };

            string notApplicableReason = "n/a";
            string assetGroupConfig = "abc.ss";
            string variantConfig = "123.ss";


            var auditLog = new AuditLogRecord(
                commandId,
                timestamp,
                agentId,
                assetGroupId,
                assetGroupQualifier,
                action,
                rowCount,
                variantsApplied,
                exceptions,
                commandType,
                notApplicableReason,
                assetGroupConfig,
                variantConfig);

            string auditLogString = auditLog.ToCosmosRawString();

            string[] splittedString = auditLogString.Split('\t');

            // The count should matches the cosmos stream column count. 
            Assert.Equal(13, splittedString.Length);

            Assert.Equal(commandId.GuidValue.ToString("d"), splittedString[0]);
            Assert.Equal(timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[1]);
            Assert.Equal(agentId.GuidValue.ToString("d"), splittedString[2]);
            Assert.Equal(assetGroupId.GuidValue.ToString("d"), splittedString[3]);
            Assert.Equal(assetGroupQualifier, HttpUtility.UrlDecode(splittedString[4]));
            Assert.Equal(action.ToString(), splittedString[5]);
            Assert.Equal(rowCount.ToString(), splittedString[6]);
            Assert.Equal(exceptions, HttpUtility.UrlDecode(splittedString[8]));
            Assert.Equal("Delete", splittedString[9]);
            Assert.Equal(notApplicableReason, splittedString[10]);
            Assert.Equal(assetGroupConfig, splittedString[11]);
            Assert.Equal(variantConfig, splittedString[12]);

            string variantsString = $"[\"{variant1}\",\"{variant2}\"]";
            Assert.Equal(variantsString, splittedString[7]);

            // Test with value truncated.
            auditLogString = auditLog.ToCosmosRawString(20);

            splittedString = auditLogString.Split('\t');
            Assert.Equal(13, splittedString.Length);

            string truncatedExceptions = $"{exceptions.Substring(0, 20)}[TRUNCATED]...";
            Assert.Equal(truncatedExceptions, HttpUtility.UrlDecode(splittedString[8]));
        }
    }
}
