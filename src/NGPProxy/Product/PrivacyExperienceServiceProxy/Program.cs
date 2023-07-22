// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Diagnostics;
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;
    using Microsoft.AspNet.OData.Extensions;

    using global::Owin;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Owin;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Handlers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.ODataConfigs;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    ///     The PXS Proxy Program
    /// </summary>
    public class Program : HostApplication
    {
        /// <summary>
        ///     The Main method of execution.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            using (var program = new Program())
            {
                program.Run(args);
            }
        }

        /// <summary>
        ///     Runs the program
        /// </summary>
        public void Run(string[] args)
        {
            try
            {
                ResetTraceDecorator.ResetTraceListeners();
                ResetTraceDecorator.AddConsoleTraceListener();

                SetupServiceDependencies(out IPrivacyConfigurationManager configurationManager, out ILogger logger, out DependencyManager dependencyManager);

                HostDecorator servicePointManagerDecorator = new ServicePointDecorator(configurationManager.PrivacyExperienceServiceConfiguration);
                HostDecorator httpsEndpoint = new HttpsEndpointDecorator(
                    appBuilder =>
                    {
                        appBuilder.Use(typeof(NoSniffXContentTypeOptionsMiddleware));
                        appBuilder.UseWebApi(CreateHttpConfiguration(dependencyManager, configurationManager, logger));
                    });

                // Startup pipeline components execute from left to right
                IHost serviceHost = HostFactory.CreatePipeline(
                    new ExceptionDecorator(args),
                    servicePointManagerDecorator,
                    httpsEndpoint,
                    new StartupSuccessDecorator(),
                    new RemoveConsoleLoggerDecorator());
                serviceHost.Execute();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Unhandled exception: {e}");
                throw;
            }
            finally
            {
                this.Shutdown();
            }

            // Prevents console from closing immediately when debugging
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();
            }
        }

        private static HttpConfiguration CreateHttpConfiguration(DependencyManager dependencyResolver, IPrivacyConfigurationManager configurationManager, ILogger logger)
        {
            var config = new HttpConfiguration
            {
                DependencyResolver = dependencyResolver
            };

            // Use Web API 2 Attribute Routing for specifying API paths in each controller
            config.MapHttpAttributeRoutes();

            // Enable ODATA
            config.MapODataServiceRoute("ODataRoute", null, ModelBuilder.GetEdmModel());

            // Register a global exception handler
            config.Services.Replace(typeof(IExceptionHandler), new UnhandledExceptionHandler());

            config.MessageHandlers.Add(new IncomingLogicalOperationHandler(dependencyResolver.Container.Resolve<IMachineIdRetriever>()));
            config.MessageHandlers.Add(new PerfCounterHandler(dependencyResolver.Container.Resolve<ICounterFactory>()));

            config.Filters.Add(new UnhandledExceptionFilterAttribute(logger));
            config.Filters.Add(new AuthorizeAttribute());

            var authenticationFilter = new PrivacyExperienceAuthenticationFilter(
                dependencyResolver.Container.Resolve<IRpsAuthServer>(),
                logger,
                dependencyResolver.Container.Resolve<IMsaIdentityServiceAdapter>(),
                dependencyResolver.Container.Resolve<ICertificateValidator>(),
                dependencyResolver.Container.Resolve<IAadAuthManager>(),
                dependencyResolver.Container.Resolve<IFamilyClaimsParser>(),
                dependencyResolver.Container.Resolve<ICustomerMasterAdapter>(),
                dependencyResolver.Container.Resolve<IAppConfiguration>());
            config.Filters.Add(authenticationFilter);

            config.Initializer(config);

            return config;
        }

        private static void SetupServiceDependencies(
            out IPrivacyConfigurationManager configurationManager,
            out ILogger logger,
            out DependencyManager dependencyResolver)
        {
            logger = DualLogger.Instance;
            DualLogger.AddTraceListener();

            Trace.TraceInformation($"Executing method: {nameof(SetupServiceDependencies)}");
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

            var container = new UnityContainer();
            container.RegisterInstance<ILogger>(logger);

            IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);

            dependencyResolver = new DependencyManager(container, configLoader);
            configurationManager = configLoader.CurrentConfiguration;
        }
    }
}
