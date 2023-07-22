namespace Microsoft.PrivacyServices.DataManagement.Common.Autofac
{
    using System;

    using global::Autofac;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Telemetry;

    /// <summary>
    /// Registers dependencies for Instrumentation <c>Autofac</c>.
    /// </summary>
    public class InstrumentationModule : Module
    {
        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CorrelationVector>().As<ICorrelationVector>().InstancePerLifetimeScope();
            builder.RegisterType<SessionProperties>().InstancePerLifetimeScope();
            builder.RegisterType<SessionWriterFactory>().As<ISessionWriterFactory>().InstancePerLifetimeScope();
            builder.RegisterType<SessionFactory>().As<ISessionFactory>().InstancePerLifetimeScope();

            builder.RegisterType<EventWriterFactory>().As<IEventWriterFactory>().InstancePerLifetimeScope();

            // Register an EventWriter and SessionWriter to handle any unmapped events.
            builder.RegisterGeneric(typeof(EmptyEventWriter<>)).As(typeof(IEventWriter<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(EmptySessionWriter<>)).As(typeof(ISessionWriter<>)).InstancePerLifetimeScope();

            // Register specific EventWriters and SessionWriters.
            builder.RegisterType<StringEventWriter>().As<IEventWriter<string>>().InstancePerLifetimeScope();
            builder.RegisterType<SuppressedExceptionEventWriter>().As<IEventWriter<SuppressedException>>().InstancePerLifetimeScope();
            builder.RegisterType<InconsistentStateEventWriter>().As<IEventWriter<InconsistentState>>().InstancePerLifetimeScope();

            // Register a different NullEvent writer per SessionType.
            builder
                .RegisterType<ExceptionSessionWriter>()
                .Keyed<ISessionWriter<Exception>>(SessionType.Incoming)
                .WithParameter("sessionType", SessionType.Incoming)
                .InstancePerLifetimeScope();

            builder
                .RegisterType<ExceptionSessionWriter>()
                .Keyed<ISessionWriter<Exception>>(SessionType.Outgoing)
                .WithParameter("sessionType", SessionType.Outgoing)
                .InstancePerLifetimeScope();

            builder
                .RegisterType<ExceptionSessionWriter>()
                .Keyed<ISessionWriter<Exception>>(SessionType.Internal)
                .WithParameter("sessionType", SessionType.Internal)
                .InstancePerLifetimeScope();

            ILogger<Base> sllLogger = new SllLogger();
            IPrivacyConfigurationManager privacyConfigurationManager = PrivacyConfigurationManager.LoadCurrentConfiguration();
            builder.RegisterInstance(privacyConfigurationManager).As<IPrivacyConfigurationManager>().SingleInstance();
           
            builder.RegisterInstance(sllLogger).As<ILogger<Base>>().SingleInstance();
            builder.RegisterInstance(DualLogger.Instance).As<PrivacyServices.Common.Azure.ILogger>().SingleInstance();
        }
    }
}
