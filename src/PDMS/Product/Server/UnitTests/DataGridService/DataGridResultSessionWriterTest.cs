namespace Microsoft.PrivacyServices.DataManagement.DataGridService.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using global::Autofac;
    using Microsoft.DataPlatform.DataDiscoveryService.Contracts;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataGridService;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataGridResultSessionWriterTest
    {
        [Theory(DisplayName = "When data grid result session writer is called, the logger should be called."), AutoMoqData]
        public void VerifyDataGridResultSessionWriter(
            [Frozen] Mock<ILogger<Base>> logger,
            SessionProperties properties,
            SessionStatus status,
            Tuple<SearchResponse, string, string> data)
        {
            var writer = new DataGridResultSessionWriter(logger.Object, properties);
            writer.WriteDone(status, "name", 10L, "cv", data);

            logger.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<DataGridResultEvent>(), It.IsAny<System.Diagnostics.Tracing.EventLevel>(), EventOptions.None, It.IsAny<string>()), Times.Once);
        }

        [Theory(DisplayName = "Verify DataGridResultEvent."), AutoMoqData]
        public void VerifyConversionForDataGridResult(Mock<ILogger<Base>> log, SearchResponse response, string assetType, string searchTerm)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);

            Tuple<SearchResponse, string, string> data = new Tuple<SearchResponse, string, string>(response, assetType, searchTerm);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataGridResultEvent> verify = v =>
            {
                Assert.Equal(response.TotalHits, v.totalHits);
                Assert.Equal(assetType, v.assetType);
                Assert.Equal(searchTerm, v.searchTerms);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        [Theory(DisplayName = "When a DataGridResult is returned, log it as a success."), AutoMoqData]
        public void VerifyConversionForDataGridSearchResult(Mock<ILogger<Base>> log, Tuple<SearchResponse, string, string> data)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataGridResultEvent> verify = v =>
            {
                Assert.Equal(data.Item1.TotalHits, v.totalHits);
                Assert.Equal(data.Item2, v.assetType);
                Assert.Equal(data.Item3, v.searchTerms);
                Assert.Equal("success", v.baseData.protocolStatusCode);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, string.Empty), Times.Once());
        }

        private ISessionWriterFactory CreateSessionFactory(Mock<ILogger<Base>> log)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataGridModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            return builder.Build().Resolve<ISessionWriterFactory>();
        }
    }
}