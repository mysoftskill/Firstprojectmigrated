namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.AnaheimAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::Azure.Messaging.EventHubs;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class EventHubAIdAdapterTests
    {
        private static readonly DeleteDeviceIdRequest deleteDeviceIdRequest = new DeleteDeviceIdRequest
        {
            AuthorizationId = "AuthorizationId",
            CorrelationVector = "CorrelationVector",
            GlobalDeviceId = 123456,
            RequestId = Guid.NewGuid(),
            CreateTime = DateTimeOffset.UtcNow,
            TestSignal = true
        };

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(EventHubAIdAdapterConstructorTestData), DynamicDataSourceType.Method)]
        public void EventHubAdapterShouldHandleNull(IEventHubProducer eventHubProducer, ILogger logger)
        {
            new EventHubAIdAdapter(eventHubProducer, logger);
        }

        [TestMethod]
        public async Task SendDeleteDeviceIdRequestShouldSucceed()
        {
            // Arrange
            Mock<IEventHubProducer> mockEventHubProducer = new Mock<IEventHubProducer>(MockBehavior.Strict);
            mockEventHubProducer.Setup(m => m.SendAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            IAnaheimIdAdapter eventHubAIdAdapter = new EventHubAIdAdapter(mockEventHubProducer.Object, new ConsoleLogger());

            // Act
            var response = await eventHubAIdAdapter.SendDeleteDeviceIdRequestAsync(deleteDeviceIdRequest).ConfigureAwait(false);

            // Assert
            mockEventHubProducer.Verify(a => a.SendAsync(It.IsAny<string>()), Times.Exactly(1));
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        [DynamicData(nameof(EventHubAIdAdapterExceptionTestData), DynamicDataSourceType.Method)]
        public async Task SendDeleteDeviceIdRequestShouldHandleExceptions(Exception exception, AdapterErrorCode adapterErrorCode, int statusCode, string expectedErrorMsg)
        {
            // Arrange
            Mock<IEventHubProducer> mockEventHubProducer = new Mock<IEventHubProducer>(MockBehavior.Strict);
            mockEventHubProducer.Setup(m => m.SendAsync(It.IsAny<string>())).Throws(exception);
            IAnaheimIdAdapter eventHubAIdAdapter = new EventHubAIdAdapter(mockEventHubProducer.Object, new ConsoleLogger());

            // Act
            var response = await eventHubAIdAdapter.SendDeleteDeviceIdRequestAsync(deleteDeviceIdRequest).ConfigureAwait(false);

            // Assert
            mockEventHubProducer.Verify(a => a.SendAsync(It.IsAny<string>()), Times.Exactly(1));
            Assert.AreEqual(adapterErrorCode, response.Error.Code);
            Assert.AreEqual(statusCode, response.Error.StatusCode);
            Assert.AreEqual(expectedErrorMsg, response.Error.Message);
        }

        #region Test Data

        public static IEnumerable<object[]> EventHubAIdAdapterConstructorTestData()
        {
            Mock<IEventHubProducer> mockEventHubProducer = new Mock<IEventHubProducer>(MockBehavior.Strict);
            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    new ConsoleLogger()
                },
                 new object[]
                {
                    mockEventHubProducer.Object,
                    null
                }
            };
            return data;
        }

        public static IEnumerable<object[]> EventHubAIdAdapterExceptionTestData()
        {
            var data = new List<object[]>
            {
                new object[]
                {
                    new JsonSerializationException("Failed to serialize message."),
                    AdapterErrorCode.JsonDeserializationFailure,
                    500,
                    "Failed to serialize message."
                },
                new object[]
                {
                    new EventHubsException(false, "EventHubName", EventHubsException.FailureReason.ResourceNotFound),
                    AdapterErrorCode.Unknown,
                    500,
                    $"Error message: Exception of type 'Azure.Messaging.EventHubs.EventHubsException' was thrown. (EventHubName). Failed Reason: ResourceNotFound"
                },
                new object[]
                {
                    new Exception("Error from Evenhub Producer"),
                    AdapterErrorCode.Unknown,
                    500,
                    "Error from Evenhub Producer"
                }
            };
            return data;
        }

        #endregion
    }
}
