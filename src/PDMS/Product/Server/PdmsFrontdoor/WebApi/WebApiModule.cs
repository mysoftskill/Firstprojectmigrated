namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System;
    using System.Web.Http.ExceptionHandling;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling;

    using WebApiThrottle;
    using WebApiThrottle.Net;

    public class WebApiModule<T> : Module
        where T : IRequestContextFactory
    {
        /// <summary>
        /// Registers dependencies for WebApi with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            // Register WebApi specific event writers.
            builder.RegisterType<SuccessSessionWriter>().As<ISessionWriter<OperationMetadata>>().InstancePerLifetimeScope();
            builder.RegisterType<ServiceErrorSessionWriter>().As<ISessionWriter<Tuple<OperationMetadata, ServiceError>>>().InstancePerLifetimeScope();

            builder.RegisterType<ServiceExceptionHandler>().As<IExceptionHandler>().InstancePerLifetimeScope();
            builder.RegisterType<T>().As<IRequestContextFactory>().SingleInstance();
            builder.RegisterType<DefaultOperationNameProvider>().As<IOperationNameProvider>().PreserveExistingDefaults().SingleInstance();
            builder.RegisterType<DefaultOperationAccessProvider>().As<IOperationAccessProvider>().PreserveExistingDefaults().SingleInstance();

            // Registers the probe check that we want. At the moment, we always use the same standard check.
            // This can be overriden by registering a new instance for ProbeAsync in specific services.
            builder.RegisterInstance(new DefaultProbe()).As<IProbeMonitor>();
            builder.RegisterType<ProbeHandler>().As<ProbeHandler>().SingleInstance();
            builder.RegisterType<OpenApiHandler>().As<OpenApiHandler>().SingleInstance();
            builder.RegisterType<ServerErrorThrottleHandler>().As<ThrottlingHandler>().SingleInstance();
            builder.RegisterType<AuthenticationFilter>().As<AuthenticationFilter>().InstancePerLifetimeScope();
            builder.RegisterType<AuthorizationFilter>().As<AuthorizationFilter>().InstancePerLifetimeScope();
            builder.RegisterType<AuthenticatedPrincipal>().As<AuthenticatedPrincipal>().InstancePerLifetimeScope();

            // Register throttling mechanism.
            builder.RegisterType<ThrottleLogger>().As<IThrottleLogger>().SingleInstance();
            builder.RegisterInstance(new DefaultIpAddressParser()).As<IIpAddressParser>().SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return new ParallaxThrottlePolicyProvider(configuration.ThrottlingConfiguration);
                })
                .As<IThrottlePolicyProvider>()
                .SingleInstance();

            builder.Register(ctx => ThrottlePolicy.FromStore(ctx.Resolve<IThrottlePolicyProvider>())).SingleInstance();
            builder.RegisterInstance(new PolicyMemoryCacheRepository()).As<IPolicyRepository>().SingleInstance();
            builder.RegisterInstance(new MemoryCacheRepository()).As<IThrottleRepository>().SingleInstance();
        }
    }
}
