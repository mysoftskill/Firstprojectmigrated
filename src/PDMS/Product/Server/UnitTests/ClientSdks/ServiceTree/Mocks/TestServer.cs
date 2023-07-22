namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using Moq;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using WebApiThrottle;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling;

    public static class TestServer
    {
        public static Microsoft.Owin.Testing.TestServer Create(IContainer dependencies)
        {
            var apiRegistrations = new ApiRegistration[] { ServiceController.Register };
            
            var testServer = Microsoft.Owin.Testing.TestServer.Create(builder => OwinStartup.Register(builder, dependencies, apiRegistrations));
            testServer.BaseAddress = new Uri("https://localhost");
            return testServer;
        }

        public static IContainer CreateDependencies(Action<ContainerBuilder> registerDependencies = null)
        { 
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new WebApiModule<OwinRequestContextFactory>());

            //containerBuilder.RegisterInstance((new Mock<IThrottlingConfiguration>()).Object).As<IThrottlingConfiguration>();

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

            containerBuilder.RegisterModule(new InstrumentationModule());
            //containerBuilder.RegisterModule(new RegistrationModule(null)); // frontdoor
            OwinModule.RegisterOwin(containerBuilder);

            containerBuilder.RegisterModule(new DataAccessModule());

            containerBuilder.RegisterInstance<ILogger<Microsoft.Telemetry.Base>>(new Mock<ILogger<Telemetry.Base>>().Object);
            registerDependencies?.Invoke(containerBuilder); // Register any dependencies to override the automatic registrations.

            return containerBuilder.Build();
        }

        public static HttpResponseMessage LoadResponse(HttpStatusCode statusCode, string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return new HttpResponseMessage()
                {
                    StatusCode = statusCode
                };
            }
            else
            {
                var type = typeof(ServiceClientTest);
                var assembly = type.Assembly;
                var names = assembly.GetManifestResourceNames();

                foreach (var name in names)
                {
                    if (name.EndsWith(filename))
                    {
                        filename = name;
                        break;
                    }
                }

                var stream = assembly.GetManifestResourceStream(filename);

                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();

                    return new HttpResponseMessage()
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(content, Encoding.UTF8, "application/json")
                    };
                }
            }
        }
    }
}