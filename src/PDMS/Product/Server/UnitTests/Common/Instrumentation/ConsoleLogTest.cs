namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Newtonsoft.Json;

    using Xunit;

    public class ConsoleLogTest
    {
        private const ConsoleColor DefaultColor = ConsoleColor.Magenta;

        [Theory(DisplayName = "Verify the data and color written by ConsoleLog")]
        [InlineAutoMoqData(EventLevel.Critical, ConsoleColor.Red)]
        [InlineAutoMoqData(EventLevel.Error, ConsoleColor.Red)]
        [InlineAutoMoqData(EventLevel.Informational, DefaultColor)]
        [InlineAutoMoqData(EventLevel.LogAlways, DefaultColor)]
        [InlineAutoMoqData(EventLevel.Verbose, ConsoleColor.DarkGray)]
        [InlineAutoMoqData(EventLevel.Warning, ConsoleColor.Yellow)]
        public void VerifyWrite(
            EventLevel level, 
            ConsoleColor color,
            Mock<ILogger<int>> defaultLog, 
            SessionProperties properties, 
            int data, 
            EventOptions options,
            string cv)
        {
            // Arrange
            var consoleWriter = new Mock<IConsoleWriter>();
            var consoleLog = new ConsoleLog<int>(defaultLog.Object, DefaultColor) { ConsoleWriter = consoleWriter.Object };

            // Act
            consoleLog.Write(properties, data, level, options, cv);

            // Assert
            var finalData = new ConsoleLog<int>.Data<int> { Event = data, Properties = properties, CV = cv };
            var finalDataString = JsonConvert.SerializeObject(finalData, Formatting.Indented, ConsoleLog<int>.SerializationSettings);

            defaultLog.Verify(m => m.Write(properties, data, level, options, cv), Times.Once());
            consoleWriter.Verify(m => m.WriteLine(finalDataString, color), Times.Once());
            consoleWriter.Verify(m => m.WriteLine(ConsoleLog<int>.Separator, DefaultColor), Times.Once());
        }
    }
}
