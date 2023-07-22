namespace PCF.UnitTests
{
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class StorageConverterTests : INeedDataBuilders
    {
        [Fact]
        public void SerializeAndParse_Delete_MSA()
        {
            DeleteCommand deleteCommand = this.ADeleteCommand().With(x => x.Subject, this.AnMsaSubject().Build());
            this.SerializeAndParseCommand(deleteCommand);
        }

        [Fact]
        public void SerializeAndParse_Export_AAD()
        {
            ExportCommand deleteCommand = this.AnExportCommand().With(x => x.Subject, this.AnAadSubject().Build());
            this.SerializeAndParseCommand(deleteCommand);
        }

        [Fact]
        public void SerializeAndParse_AccountClose_Device()
        {
            AccountCloseCommand command = this.AnAccountCloseCommand().With(x => x.Subject, this.ADeviceSubject().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_AgeOut_MSA()
        {
            AgeOutCommand command = this.AnAgeOutCommand().With(x => x.Subject, this.AnMsaSubject().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_Demographic()
        {
            DeleteCommand command = this.ADeleteCommand().With(x => x.Subject, this.ADemographicSubject().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_BrowsingHistory()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.BrowsingHistory, this.APredicate<BrowsingHistoryPredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_SearchRequestsAndQuery()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.SearchRequestsAndQuery, this.APredicate<SearchRequestsAndQueryPredicate>().Build());
            this.SerializeAndParseCommand(command);
        }
        
        [Fact]
        public void SerializeAndParse_Delete_ITSU()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance, this.APredicate<InkingTypingAndSpeechUtterancePredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_PSU()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ProductAndServiceUsage, this.APredicate<ProductAndServiceUsagePredicate>().Build());
            this.SerializeAndParseCommand(command);
        }
        
        [Fact]
        public void SerializeAndParse_Delete_ContentConsumption()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ContentConsumption, this.APredicate<ContentConsumptionPredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_PSP()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ProductAndServicePerformance, this.APredicate<ProductAndServicePerformancePredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_SSI()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory, this.APredicate<SoftwareSetupAndInventoryPredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        [Fact]
        public void SerializeAndParse_Delete_DCC()
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration, this.APredicate<DeviceConnectivityAndConfigurationPredicate>().Build());
            this.SerializeAndParseCommand(command);
        }

        private void SerializeAndParseCommand(PrivacyCommand command)
        {
            StorageCommandSerializer storageCommandConverter = new StorageCommandSerializer();
            StoragePrivacyCommand storageCommand = storageCommandConverter.Process(command);

            string json = JsonConvert.SerializeObject(storageCommand);
            Document document = JsonConvert.DeserializeObject<Document>(json);

            PrivacyCommand parsedAgain = new StorageCommandParser(command.AgentId, command.AssetGroupId, QueueStorageType.Undefined).Process(document);

            Assert.Equal(command.CommandType, parsedAgain.CommandType);

            string resultJson = JsonConvert.SerializeObject(storageCommandConverter.Process(parsedAgain));

            Assert.Equal(json, resultJson);
        }
    }
}
