namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client.SharedCommandFeedContracts.Partials;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class PrivacyCommandFeedParserTest
    {
        [TestMethod]
        public void TestDisambiguationDelete()
        {
            DeleteCommand testCommand = new DeleteCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 1,
                    Anid = "2",
                    Cid = 3,
                    Opid = "4",
                    Xuid = "5",
                },
                "agentState",
                null,
                Policies.Current.DataTypes.Ids.ContentConsumption,
                new TimeRangePredicate
                {
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                },
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);

            var json = JObject.FromObject(testCommand);
            var parsedCommand = PrivacyCommandFeedParser.ParseObject(json);        
            Assert.IsNotNull(parsedCommand);

            DeleteCommand deleteCommand = parsedCommand as DeleteCommand;
            Assert.IsNotNull(deleteCommand);
            Assert.AreEqual(testCommand.CommandId, deleteCommand.CommandId);
            Assert.AreEqual(testCommand.DataTypePredicate, deleteCommand.DataTypePredicate);
        }

        [TestMethod]
        public void TestDisambiguationExport()
        {
            ExportCommand testCommand = new ExportCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 1,
                    Anid = "2",
                    Cid = 3,
                    Opid = "4",
                    Xuid = "5",
                },
                "agentState",
                new DataTypeId[]
                {
                    Policies.Current.DataTypes.Ids.ContentConsumption
                },
                new Uri("https://tempuri.org/exportme"),
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);

            var json = JObject.FromObject(testCommand);

            var parsedCommand = PrivacyCommandFeedParser.ParseObject(json);
            Assert.IsNotNull(parsedCommand);

            ExportCommand exportCommand = parsedCommand as ExportCommand;
            Assert.IsNotNull(exportCommand);
            Assert.AreEqual(testCommand.CommandId, exportCommand.CommandId);
            Assert.AreEqual(testCommand.AzureBlobContainerTargetUri, exportCommand.AzureBlobContainerTargetUri);
        }

        [TestMethod]
        public void TestDisambiguationAccountClose()
        {
            AccountCloseCommand testCommand = new AccountCloseCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 1,
                    Anid = "2",
                    Cid = 3,
                    Opid = "4",
                    Xuid = "5",
                },
                "agentState",
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);

            var json = JObject.FromObject(testCommand);

            var command = PrivacyCommandFeedParser.ParseObject(json);

            Assert.IsNotNull(command);
            Assert.IsTrue(command is AccountCloseCommand);
            Assert.AreEqual(testCommand.CommandId, command.CommandId);
        }

        [TestMethod]
        public void TestDisambiguationAgeOut()
        {
            AgeOutCommand testCommand = new AgeOutCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 1,
                    Anid = "2",
                    Cid = 3,
                    Opid = "4",
                    Xuid = "5",
                },
                "agentState",
                null,
                DateTimeOffset.UtcNow.AddYears(-10),
                false,
                Policies.Current.CloudInstances.Ids.Public.Value);

            var json = JObject.FromObject(testCommand);

            var command = PrivacyCommandFeedParser.ParseObject(json);

            Assert.IsNotNull(command);
            Assert.IsTrue(command is AgeOutCommand);
            Assert.AreEqual(testCommand.CommandId, command.CommandId);
        }
    }
}
