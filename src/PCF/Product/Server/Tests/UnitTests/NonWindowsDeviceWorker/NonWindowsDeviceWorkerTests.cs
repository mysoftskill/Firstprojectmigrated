namespace PCF.UnitTests.NonWindowsDeviceWorker
{
    using Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker;
    using Microsoft.Azure.ComplianceServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using PCF.UnitTests;
    using System;
    using Xunit;

    /// <summary>
    /// NonWindowsDeviceWorker unittests
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class NonWindowsDeviceWorkerTests : INeedDataBuilders
    {
        [Fact(DisplayName = "Make sure helpers create delete requests correctly.")]
        public void CreatePxsDeleteRequest_BasicTest()
        {
            Guid macOsDeviceId = Guid.NewGuid();
            string deviceid = $"u:{macOsDeviceId.ToString().ToUpperInvariant()}";
            string userId = "CoolUser";
            string cV = "cvSamplexyz.01";
            DateTimeOffset dateTime = DateTimeOffset.UtcNow;
            Guid requestGuid = Guid.NewGuid();

            string json = NonWindowsDeviceTestHelpers.CreateNonWindowsDeviceEvent(
                deviceId: deviceid,
                userId: userId,
                cV: cV,
                time: dateTime);

            foreach (var dataTypeId in NonWindowsDeviceDeleteHelpers.SupportedDataTypeIds)
            {
                var deleteRequest = NonWindowsDeviceDeleteHelpers.CreateDeleteRequestFromJson(
                    json: json,
                    requestGuid: requestGuid,
                    dataTypeId);

                VerifyCreatePxsDeleteRequest(requestGuid, deviceid, userId, cV, dateTime, dataTypeId, deleteRequest);
            }
        }

        private static void VerifyCreatePxsDeleteRequest(Guid requestGuid, string deviceid, string userId, string cV, DateTimeOffset dateTime, DataTypeId dataTypeId, DeleteRequest deleteRequest)
        {
            Assert.Equal(userId, deleteRequest.AuthorizationId);
            Assert.Equal(RequestType.Delete, deleteRequest.RequestType);
            Assert.True(deleteRequest.Subject is NonWindowsDeviceSubject);
            Assert.Equal(deviceid, (deleteRequest.Subject as NonWindowsDeviceSubject).AsimovMacOsPlatformDeviceId);
            Assert.Equal(dataTypeId.Value, deleteRequest.PrivacyDataType);
            Assert.Equal(requestGuid, deleteRequest.RequestGuid);
            Assert.Equal(cV, deleteRequest.CorrelationVector);
            Assert.Equal(dateTime, deleteRequest.TimeRangePredicate.EndTime);


            // is there a better way to do this?
            if (deleteRequest.Predicate is DeviceConnectivityAndConfigurationPredicate)
            {
                var predicate = deleteRequest.Predicate as DeviceConnectivityAndConfigurationPredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
            else if (deleteRequest.Predicate is ProductAndServiceUsagePredicate)
            {
                var predicate = deleteRequest.Predicate as ProductAndServiceUsagePredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
            else if (deleteRequest.Predicate is ProductAndServicePerformancePredicate)
            {
                var predicate = deleteRequest.Predicate as ProductAndServicePerformancePredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
            else if (deleteRequest.Predicate is SoftwareSetupAndInventoryPredicate)
            {
                var predicate = deleteRequest.Predicate as SoftwareSetupAndInventoryPredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
            else if (deleteRequest.Predicate is BrowsingHistoryPredicate)
            {
                var predicate = deleteRequest.Predicate as BrowsingHistoryPredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
            else if (deleteRequest.Predicate is InkingTypingAndSpeechUtterancePredicate)
            {
                var predicate = deleteRequest.Predicate as InkingTypingAndSpeechUtterancePredicate;
                Assert.True(predicate.WindowsDiagnosticsDeleteOnly);
            }
        }
    }
}