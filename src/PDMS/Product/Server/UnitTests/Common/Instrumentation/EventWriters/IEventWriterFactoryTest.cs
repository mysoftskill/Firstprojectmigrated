namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System.Diagnostics.Tracing;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture;

    using Xunit;

    public class IEventWriterFactoryTest
    {
        [Theory(DisplayName = "When eventWriterFactory.WriteEvent is called and an eventWriter is registerd, then call its write function."), AutoMoqData]
        public void VerifyWriteEvent_WriterFound(Fixture fixture)
        {
            // Arrange.
            var eventWriter = fixture.Create<Mock<IEventWriter<int>>>();
            var eventCreation = eventWriter.Object;

            var eventWriterFactory = new Mock<IEventWriterFactory>();
            eventWriterFactory.Setup(m => m.TryCreate(out eventCreation)).Returns(true);
            fixture.Inject(eventWriterFactory.Object);

            var value = fixture.Create<int>();

            // Act.
            eventWriterFactory.Object.WriteEvent(nameof(IEventWriterFactoryTest), value);

            // Assert.
            eventWriter.Verify(m => m.WriteEvent(nameof(IEventWriterFactoryTest), value, EventLevel.Informational, EventOptions.None), Times.Once());
        }

        [Theory(DisplayName = "When eventWriterFactory.WriteEvent is called and no eventWriter is registerd, then do not fail."), AutoMoqData]
        public void VerifyWriteEvent_WriterNotFound(Fixture fixture)
        {
            // Arrange.
            var eventWriter = fixture.Create<Mock<IEventWriter<int>>>();
            var eventCreation = eventWriter.Object;

            var eventWriterFactory = new Mock<IEventWriterFactory>();
            eventWriterFactory.Setup(m => m.TryCreate(out eventCreation)).Returns(false);
            fixture.Inject(eventWriterFactory.Object);

            var value = fixture.Create<int>();

            // Act.
            eventWriterFactory.Object.WriteEvent(nameof(IEventWriterFactoryTest), value);

            // Assert.
            eventWriter.Verify(m => m.WriteEvent(nameof(IEventWriterFactoryTest), It.IsAny<int>(), It.IsAny<EventLevel>(), It.IsAny<EventOptions>()), Times.Never());
        }
    }
}
