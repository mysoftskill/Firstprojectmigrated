// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class QueueStorageCommandConverterTest : INeedDataBuilders
    {
        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_MSA(CompressionType compressionType)
        {
            DeleteCommand deleteCommand = this.ADeleteCommand().With(x => x.Subject, this.AnMsaSubject().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(deleteCommand, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Export_AAD(CompressionType compressionType)
        {
            ExportCommand command = this.AnExportCommand().With(x => x.Subject, this.AnAadSubject().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);

        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_AccountClose_Device(CompressionType compressionType)
        {
            AccountCloseCommand command = this.AnAccountCloseCommand().With(x => x.Subject, this.ADeviceSubject().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_AgeOut_MSA(CompressionType compressionType)
        {
            AgeOutCommand command = this.AnAgeOutCommand().With(x => x.Subject, this.AnMsaSubject().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_Demographic(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().With(x => x.Subject, this.ADemographicSubject().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_BrowsingHistory(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.BrowsingHistory, this.APredicate<BrowsingHistoryPredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_SearchRequestsAndQuery(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.SearchRequestsAndQuery, this.APredicate<SearchRequestsAndQueryPredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_ITSU(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance, this.APredicate<InkingTypingAndSpeechUtterancePredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_PSU(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ProductAndServiceUsage, this.APredicate<ProductAndServiceUsagePredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_ContentConsumption(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ContentConsumption, this.APredicate<ContentConsumptionPredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_PSP(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.ProductAndServicePerformance, this.APredicate<ProductAndServicePerformancePredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_SSI(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory, this.APredicate<SoftwareSetupAndInventoryPredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        [Theory]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.None)]
        public void QueueStorageSerializeAndParse_Delete_DCC(CompressionType compressionType)
        {
            DeleteCommand command = this.ADeleteCommand().WithDataType(Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration, this.APredicate<DeviceConnectivityAndConfigurationPredicate>().Build());
            this.QueueStorageQueueStorageSerializeAndParseCommand(command, compressionType);
        }

        private void QueueStorageQueueStorageSerializeAndParseCommand(PrivacyCommand command, CompressionType compressionType)
        {
            StorageCommandSerializer storageCommandConverter = new StorageCommandSerializer();
            StoragePrivacyCommand storageCommand = storageCommandConverter.Process(command);
            CloudQueueMessage cloudQueueMessage = QueueStorageCommandConverter.ToCloudQueueMessage(storageCommand, compressionType: compressionType);
            StoragePrivacyCommand storageCommandParsed = QueueStorageCommandConverter.FromCloudQueueMessage(cloudQueueMessage);
            CloudQueueMessage cloudQueueMessageParsedAgain = QueueStorageCommandConverter.ToCloudQueueMessage(storageCommandParsed, compressionType: compressionType);
            StoragePrivacyCommand storagePrivacyCommandParsedAgain = QueueStorageCommandConverter.FromCloudQueueMessage(cloudQueueMessageParsedAgain);

            Assert.Equal(command.CommandId, new CommandId(storageCommand.Id));
            Assert.Equal(command.CommandType, storageCommand.CommandType);

            Assert.Equal(command.CommandId, new CommandId(storageCommandParsed.Id));
            Assert.Equal(command.CommandType, storageCommandParsed.CommandType);

            Assert.Equal(command.CommandId, new CommandId(storagePrivacyCommandParsedAgain.Id));
            Assert.Equal(command.CommandType, storagePrivacyCommandParsedAgain.CommandType);


            var originalJson = JsonConvert.SerializeObject(storageCommandParsed);
            var convertedJson = JsonConvert.SerializeObject(storagePrivacyCommandParsedAgain);

            Assert.Equal(originalJson, convertedJson);
        }
    }
}
