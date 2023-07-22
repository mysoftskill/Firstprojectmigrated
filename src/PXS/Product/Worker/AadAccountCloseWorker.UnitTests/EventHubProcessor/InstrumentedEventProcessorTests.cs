// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.EventHubProcessor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class InstrumentedEventProcessorTests
    {
        private InstrumentedEventProcessor instrumentedEvent;

        private Mock<IEventProcessor> mockEventProcessor;

        [TestMethod]
        public void CloseAsyncFail()
        {
            this.CallConstructor(true);
            var closeReason = CloseReason.Shutdown;

            //Act
            Task result = this.instrumentedEvent.CloseAsync(null, closeReason);

            //Assert
            Assert.IsTrue(result.IsFaulted);

            //Verify
            this.mockEventProcessor.Verify(c => c.CloseAsync(It.IsAny<PartitionContext>(), closeReason), Times.Once);
        }

        [TestMethod]
        public void CloseAsyncSuccess()
        {
            //Arrange
            this.CallConstructor();
            var context = new PartitionContext();
            var closeReason = CloseReason.Shutdown;

            //Act
            Task result = this.instrumentedEvent.CloseAsync(context, closeReason);

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
            Assert.IsFalse(result.IsFaulted);

            //Verify
            this.mockEventProcessor.Verify(c => c.CloseAsync(context, closeReason), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DataRow("Value cannot be null.\r\nParameter name: eventProcessor")]
        public void HandleConstructorException(string expectedMessage)
        {
            try
            {
                this.CallConstructor();
                new InstrumentedEventProcessor(null);
                Assert.Fail("Should have thrown exception");
            }
            catch (ArgumentNullException exception)
            {
                Assert.AreEqual(expectedMessage, exception.Message);
                throw;
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockEventProcessor = new Mock<IEventProcessor>();
        }

        [TestMethod]
        public void OpenAsyncSuccess()
        {
            this.CallConstructor();

            //Act
            Task result = this.instrumentedEvent.OpenAsync(new PartitionContext());

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
            Assert.IsFalse(result.IsFaulted);

            //Verify
            this.mockEventProcessor.Verify(c => c.OpenAsync(It.IsAny<PartitionContext>()), Times.Once);
        }

        [TestMethod]
        public void ProcessEventsAsyncSuccess()
        {
            this.CallConstructor();

            //Act
            Task result = this.instrumentedEvent.ProcessEventsAsync(new PartitionContext(), Enumerable.Empty<EventData>());

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
        }

        private void CallConstructor(bool withException = false)
        {
            if (withException)
                this.mockEventProcessor.Setup(c => c.CloseAsync(null, It.IsAny<CloseReason>())).Returns(Task.FromException(new Exception()));

            this.instrumentedEvent = new InstrumentedEventProcessor(this.mockEventProcessor.Object);
        }
    }
}
