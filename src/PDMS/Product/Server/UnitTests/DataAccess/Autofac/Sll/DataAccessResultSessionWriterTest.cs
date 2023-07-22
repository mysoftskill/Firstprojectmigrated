namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Runtime.InteropServices.ComTypes;
    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;
    using Ploeh.AutoFixture;
    using Xunit;

    public class DataAccessResultSessionWriterTest
    {
        [Theory(DisplayName = "Verify DataAccessResult LinkedEntityCheck sll event."), AutoMoqData]
        public void VerifyConversionForLinkedEntityCheck(Mock<ILogger<Base>> log, string accessKey, bool isLinkedToOtherEntities)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();

            int totalHits = isLinkedToOtherEntities ? 1 : 0;
            (DataAccessResult logInfo, bool result) data = (new DataAccessResult() { AccessKey = accessKey, TotalHits = totalHits }, isLinkedToOtherEntities);
            
            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataAccessResultEvent> verify = v =>
            {
                Assert.Equal(accessKey, v.accessKey);
                Assert.Equal(totalHits, v.totalHits);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        [Theory(DisplayName = "Verify DataAccessResult PendingCommandCheck sll event."), AutoMoqData]
        public void VerifyConversionForPendingCommandCheck(Mock<ILogger<Base>> log, string accessKey, bool hasPendingCommands)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();

            int totalHits = hasPendingCommands ? 1 : 0;
            (DataAccessResult logInfo, bool result) data = (new DataAccessResult() { AccessKey = accessKey, TotalHits = totalHits }, hasPendingCommands);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataAccessResultEvent> verify = v =>
            {
                Assert.Equal(accessKey, v.accessKey);
                Assert.Equal(totalHits, v.totalHits);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        [Theory(DisplayName = "Verify DataAccessResult SearchByIds sll event."), AutoMoqData]
        public void VerifyConversionForSearchByIds(Mock<ILogger<Base>> log, string accessKey)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();

            // TODO: have this create a random number of agents?
            var searchResult = new List<DataAgent>() { new DeleteAgent() };

            int totalHits = searchResult.Count;

            (DataAccessResult logInfo, List<DataAgent> result) data = (new DataAccessResult() { AccessKey = accessKey, TotalHits = totalHits }, searchResult);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataAccessResultEvent> verify = v =>
            {
                Assert.Equal(accessKey, v.accessKey);
                Assert.Equal(totalHits, v.totalHits);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        [Theory(DisplayName = "Verify DataAccessResult SearchByFilter sll event."), AutoMoqData]
        public void VerifyConversionForSearchByFilter(Mock<ILogger<Base>> log, string accessKey)
        {            
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();

            // TODO: set up some random filter results?
            var filterResult = new FilterResult<DataAgent>();

            int totalHits = filterResult.Count;

            (DataAccessResult logInfo, FilterResult<DataAgent> result) data = (new DataAccessResult() { AccessKey = accessKey, TotalHits = totalHits }, filterResult);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DataAccessResultEvent> verify = v =>
            {
                Assert.Equal(accessKey, v.accessKey);
                Assert.Equal(totalHits, v.totalHits);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }
    }
}