namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests
{
    using System;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling;

    using Moq;
    using WebApiThrottle;

    public static class TestServer
    {
        public static Microsoft.Owin.Testing.TestServer Create(IContainer dependencies)
        {
            var apiRegistrations = new ApiRegistration[] { Registration.Initialize };
            
            var testServer = Microsoft.Owin.Testing.TestServer.Create(builder => OwinStartup.Register(builder, dependencies, apiRegistrations));
            testServer.BaseAddress = new Uri("https://localhost");
            return testServer;
        }

        public static IContainer CreateDependencies(Action<ContainerBuilder> registerDependencies = null)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new WebApiModule<OwinRequestContextFactory>());

            Mock<IThrottlingConfiguration> throttlingConfiguration = new Mock<IThrottlingConfiguration>();
            throttlingConfiguration.SetupGet(m => m.EndpointThrottlingEnabled).Returns(false);
            throttlingConfiguration.SetupGet(m => m.IPThrottlingEnabled).Returns(false);
            throttlingConfiguration.SetupGet(m => m.LimitPerSecond).Returns(10);
            throttlingConfiguration.SetupGet(m => m.ClientThrottlingEnabled).Returns(false);

            containerBuilder
                .Register(ctx => new ParallaxThrottlePolicyProvider(throttlingConfiguration.Object))
                .As<IThrottlePolicyProvider>()
                .SingleInstance();

            containerBuilder.Register(ctx => ThrottlePolicy.FromStore(ctx.Resolve<IThrottlePolicyProvider>())).SingleInstance();

            containerBuilder.RegisterModule(new DataAccessModule());
            containerBuilder.RegisterModule(new RegistrationModule(null)); // frontdoor
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterAutoMapper();
            OwinModule.RegisterOwin(containerBuilder);

            containerBuilder.RegisterInstance((new Mock<IOperationAccessProvider>()).Object).As<IOperationAccessProvider>();
            containerBuilder.RegisterInstance((new Mock<IThrottlingConfiguration>()).Object).As<IThrottlingConfiguration>();

            containerBuilder.RegisterInstance((new Mock<IDataOwnerReader>()).Object).As<IDataOwnerReader>();
            containerBuilder.RegisterInstance((new Mock<IDataOwnerWriter>()).Object).As<IDataOwnerWriter>();
            containerBuilder.RegisterInstance((new Mock<IAssetGroupReader>()).Object).As<IAssetGroupReader>();
            containerBuilder.RegisterInstance((new Mock<IAssetGroupWriter>()).Object).As<IAssetGroupWriter>();
            containerBuilder.RegisterInstance((new Mock<IVariantDefinitionReader>()).Object).As<IVariantDefinitionReader>();
            containerBuilder.RegisterInstance((new Mock<IVariantDefinitionWriter>()).Object).As<IVariantDefinitionWriter>();
            containerBuilder.RegisterInstance((new Mock<IDataAgentReader>()).Object).As<IDataAgentReader>();
            containerBuilder.RegisterInstance((new Mock<IDeleteAgentReader>()).Object).As<IDeleteAgentReader>();
            containerBuilder.RegisterInstance((new Mock<IDataAgentWriter>()).Object).As<IDataAgentWriter>();
            containerBuilder.RegisterInstance((new Mock<IHistoryItemReader>()).Object).As<IHistoryItemReader>();
            containerBuilder.RegisterInstance((new Mock<ISharingRequestReader>()).Object).As<ISharingRequestReader>();
            containerBuilder.RegisterInstance((new Mock<ISharingRequestWriter>()).Object).As<ISharingRequestWriter>();
            containerBuilder.RegisterInstance((new Mock<IVariantRequestReader>()).Object).As<IVariantRequestReader>();
            containerBuilder.RegisterInstance((new Mock<IVariantRequestWriter>()).Object).As<IVariantRequestWriter>();
            containerBuilder.RegisterInstance((new Mock<ITransferRequestReader>()).Object).As<ITransferRequestReader>();
            containerBuilder.RegisterInstance((new Mock<ITransferRequestWriter>()).Object).As<ITransferRequestWriter>();

            containerBuilder.RegisterInstance<ILogger<Microsoft.Telemetry.Base>>(new Mock<ILogger<Telemetry.Base>>().Object);
            registerDependencies?.Invoke(containerBuilder); // Register any dependencies to override the automatic registrations.

            return containerBuilder.Build();
        }
    }
}
