namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class CloudQueueExceptionSessionWriterTest
    {
        [Theory(DisplayName = "Verify exception data is logged for outgoing calls."), AutoMoqData]
        public void VerifyExceptionFieldsOutgoing(Mock<ILogger<Base>> log, 
            SessionProperties properties, 
            CloudQueueEvent data,
            Exception innerExn,
            string cv)
        {
            data.Message = new Azure.Storage.Queue.CloudQueueMessage("message");
            var exn = new CloudQueueException("CloudQueue.MethodName", data, innerExn);
            var writer = new CloudQueueExceptionSessionWriter(log.Object, properties);
            writer.WriteDone(SessionStatus.Error, string.Empty, 0, cv, exn);

            Action<CloudQueueExceptionEvent> verify = e =>
            {
                Assert.Equal(data.Message?.AsString, e.message);
                Assert.Equal(data.MessageCount?.ToString(), e.messageCount);
                Assert.Equal(data.PrimaryUri, e.primaryUri);
                Assert.Equal(data.SecondaryUri, e.secondaryUri);
                Assert.Equal(data.QueueName, e.queueName);
                Assert.Equal(exn.StackTrace, e.stackTrace);
                Assert.Equal(innerExn.Message, e.innerException.message);
            };

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Warning, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "When a session writer is resolved for CloudQueueException, then return CloudQueueExceptionSessionWriter"), AutoMoqData]
        public void VerifyAutofacRegistration(ISllConfig sllConfig)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new DataAccessModule());
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterInstance(sllConfig);
            var container = containerBuilder.Build();

            var sessionWriter = container.Resolve<ISessionWriter<CloudQueueException>>();
            Assert.Equal(typeof(CloudQueueExceptionSessionWriter), sessionWriter.GetType());
        }
    }
}
