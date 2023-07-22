namespace PCF.UnitTests
{
    using System.Globalization;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    [Trait("Category", "UnitTest")]
    public class CommandRawDataReceiverTests : INeedDataBuilders
    {
        [Theory]
        [InlineAutoData(true, "True")]
        [InlineAutoData(false, "False")]
        public void DeleteCommand_ToCosmosRawStringTest(bool isTest, string expectedTestValue)
        {
            PXSV1.DeleteRequest deleteRequest = this.ADeletePxsCommand()
                .WithValue(x => x.IsTestRequest, isTest);
            bool isSerialized = CommandRawDataReceiver.TrySerializeToCosmosRawString(JObject.FromObject(deleteRequest), out string outputString);
            var splittedString = outputString.Split('\t');

            Assert.True(isSerialized);
            Assert.Equal(18, splittedString.Length);
            Assert.Equal($"[\"{deleteRequest.PrivacyDataType}\"]", splittedString[0]);
            Assert.Equal(deleteRequest.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[1]);
            Assert.Equal(deleteRequest.RequestId.ToString(), splittedString[2]);
            Assert.Equal(deleteRequest.RequestGuid.ToString(), splittedString[3]);
            Assert.Equal(deleteRequest.CorrelationVector, splittedString[4]);
            Assert.Equal(JsonConvert.SerializeObject(deleteRequest.Subject), splittedString[5]);
            Assert.Equal(deleteRequest.AuthorizationId, splittedString[6]);
            Assert.Equal(deleteRequest.TimeRangePredicate.StartTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[7]);
            Assert.Equal(deleteRequest.TimeRangePredicate.EndTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[8]);
            Assert.Equal(JsonConvert.SerializeObject(deleteRequest.Predicate), splittedString[9]);
            Assert.Equal(deleteRequest.RequestType.ToString(), splittedString[10]);
            Assert.Equal(deleteRequest.CloudInstance, splittedString[11]);
            Assert.Equal(deleteRequest.Portal, splittedString[12]);
            Assert.Equal(deleteRequest.ProcessorApplicable.ToString(), splittedString[13]);
            Assert.Equal(deleteRequest.ControllerApplicable.ToString(), splittedString[14]);
            Assert.Equal(deleteRequest.Requester, splittedString[15]);
            Assert.Equal(expectedTestValue, splittedString[16]);
            Assert.Equal(string.Empty, splittedString[17]);
        }

        [Theory]
        [InlineAutoData(true, "True")]
        [InlineAutoData(false, "False")]
        public void ExportCommand_ToCosmosRawStringTest(bool isTest, string expectedTestValue)
        {
            PXSV1.ExportRequest exportRequest = this.AnExportPxsCommand()
                .WithValue(x => x.IsTestRequest, isTest);
            bool isSerialized = CommandRawDataReceiver.TrySerializeToCosmosRawString(JObject.FromObject(exportRequest), out string outputString);
            var splittedString = outputString.Split('\t');

            var dataTypesString = string.Join("\",\"", exportRequest.PrivacyDataTypes);
            Assert.True(isSerialized);
            Assert.Equal(18, splittedString.Length);
            Assert.Equal($"[\"{dataTypesString}\"]", splittedString[0]);
            Assert.Equal(exportRequest.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[1]);
            Assert.Equal(exportRequest.RequestId.ToString(), splittedString[2]);
            Assert.Equal(exportRequest.RequestGuid.ToString(), splittedString[3]);
            Assert.Equal(exportRequest.CorrelationVector, splittedString[4]);
            Assert.Equal(JsonConvert.SerializeObject(exportRequest.Subject), splittedString[5]);
            Assert.Equal(exportRequest.AuthorizationId, splittedString[6]);
            Assert.Equal(string.Empty, splittedString[7]);
            Assert.Equal(string.Empty, splittedString[8]);
            Assert.Equal(string.Empty, splittedString[9]);
            Assert.Equal(exportRequest.RequestType.ToString(), splittedString[10]);
            Assert.Equal(exportRequest.CloudInstance, splittedString[11]);
            Assert.Equal(exportRequest.Portal, splittedString[12]);
            Assert.Equal(exportRequest.ProcessorApplicable.ToString(), splittedString[13]);
            Assert.Equal(exportRequest.ControllerApplicable.ToString(), splittedString[14]);
            Assert.Equal(exportRequest.Requester, splittedString[15]);
            Assert.Equal(expectedTestValue, splittedString[16]);
            Assert.Equal(string.Empty, splittedString[17]);
        }

        [Theory]
        [InlineAutoData(true, "True")]
        [InlineAutoData(false, "False")]
        public void AccountCloseCommand_ToCosmosRawStringTest(bool isTest, string expectedTestValue)
        {
            PXSV1.AccountCloseRequest accountCloseRequest = this.AnAccountClosePxsCommand()
                .With(x => x.CorrelationVector, null)
                .With(x => x.Portal, null)
                .WithValue(x => x.IsTestRequest, isTest);
            bool isSerialized = CommandRawDataReceiver.TrySerializeToCosmosRawString(JObject.FromObject(accountCloseRequest), out string outputString);
            var splittedString = outputString.Split('\t');
            Assert.True(isSerialized);
            Assert.Equal(18, splittedString.Length);
            Assert.Equal("[]", splittedString[0]);
            Assert.Equal(accountCloseRequest.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[1]);
            Assert.Equal(accountCloseRequest.RequestId.ToString(), splittedString[2]);
            Assert.Equal(accountCloseRequest.RequestGuid.ToString(), splittedString[3]);
            Assert.Equal("(null)", splittedString[4]);
            Assert.Equal(JsonConvert.SerializeObject(accountCloseRequest.Subject), splittedString[5]);
            Assert.Equal(accountCloseRequest.AuthorizationId, splittedString[6]);
            Assert.Equal(string.Empty, splittedString[7]);
            Assert.Equal(string.Empty, splittedString[8]);
            Assert.Equal(string.Empty, splittedString[9]);
            Assert.Equal(accountCloseRequest.RequestType.ToString(), splittedString[10]);
            Assert.Equal(accountCloseRequest.CloudInstance, splittedString[11]);
            Assert.Equal(string.Empty, splittedString[12]);
            Assert.Equal(accountCloseRequest.ProcessorApplicable.ToString(), splittedString[13]);
            Assert.Equal(accountCloseRequest.ControllerApplicable.ToString(), splittedString[14]);
            Assert.Equal(accountCloseRequest.Requester, splittedString[15]);
            Assert.Equal(expectedTestValue, splittedString[16]);
            Assert.Equal(string.Empty, splittedString[17]);
        }

        [Theory]
        [InlineAutoData(true, "True")]
        [InlineAutoData(false, "False")]
        public void AgeOutCommand_ToCosmosRawStringTest(bool isTest, string expectedTestValue)
        {
            PXSV1.AgeOutRequest ageOutRequest = this.AnAgeOutPxsCommand()
                .WithValue(x => x.IsTestRequest, isTest);

            bool isSerialized = CommandRawDataReceiver.TrySerializeToCosmosRawString(JObject.FromObject(ageOutRequest), out string outputString);
            var splittedString = outputString.Split('\t');

            Assert.True(isSerialized);
            Assert.Equal(18, splittedString.Length);
            Assert.Equal("[]", splittedString[0]);
            Assert.Equal(ageOutRequest.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[1]);
            Assert.Equal(ageOutRequest.RequestId.ToString(), splittedString[2]);
            Assert.Equal(ageOutRequest.RequestGuid.ToString(), splittedString[3]);
            Assert.Equal(ageOutRequest.CorrelationVector, splittedString[4]);
            Assert.Equal(JsonConvert.SerializeObject(ageOutRequest.Subject), splittedString[5]);
            Assert.Equal(ageOutRequest.AuthorizationId, splittedString[6]);
            Assert.Equal(string.Empty, splittedString[7]);
            Assert.Equal(string.Empty, splittedString[8]);
            Assert.Equal(string.Empty, splittedString[9]);
            Assert.Equal(ageOutRequest.RequestType.ToString(), splittedString[10]);
            Assert.Equal(ageOutRequest.CloudInstance, splittedString[11]);
            Assert.Equal(ageOutRequest.Portal, splittedString[12]);
            Assert.Equal(ageOutRequest.ProcessorApplicable.ToString(), splittedString[13]);
            Assert.Equal(ageOutRequest.ControllerApplicable.ToString(), splittedString[14]);
            Assert.Equal(ageOutRequest.Requester, splittedString[15]);
            Assert.Equal(expectedTestValue, splittedString[16]);
            Assert.Equal(ageOutRequest.LastActive.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), splittedString[17]);
        }
    }
}