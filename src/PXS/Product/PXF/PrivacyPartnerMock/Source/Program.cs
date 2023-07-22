// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService
{
    using global::Owin;

    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Practices.Unity;

    public static class Program
    {
        public static void Main(string[] args)
        {
            SetupServiceDependencies(out ILogger logger, out DependencyManager dependencyManager, out IPrivacyConfigurationManager configurationManager);

            PartnerMockConfigurations = configurationManager;

            HostDecorator resetTrace = new ResetTraceDecorator();
            HostDecorator exceptionHandler = new ExceptionDecorator(args);

            HostDecorator rpsConfigurationLoader = new RpsConfigurationDecorator(
                "Microsoft.Membership.MemberServices.PrivacyMockService.rpsserver.xml",
                Assembly.GetExecutingAssembly());

            IHost rpsServiceHost = HostFactory.CreateNonBlockingPipeline(rpsConfigurationLoader);
            rpsServiceHost.Execute();

            HttpConfiguration httpConfiguration = Startup.CreateHttpConfiguration();
            httpConfiguration.DependencyResolver = dependencyManager;

            HostDecorator familyCertificateInitializer = new FamilyDecorator(new FamilyDecoratorConfig(), logger);

            string httpsPort = "443";

            if (!IsHostedInServiceFabric)
            {
                Console.WriteLine("Running local. Using port 444.");
                httpsPort = "444";
            }

            var httpsEndpoint = new HttpsEndpointDecorator(
                appbuilder => appbuilder.UseWebApi(httpConfiguration),
                httpsPort);

            // Startup pipeline components execute from top to bottom
            IHost serviceHost = HostFactory.CreatePipeline(
                resetTrace,
                exceptionHandler,
                familyCertificateInitializer,
                httpsEndpoint);
            serviceHost.Execute();

            // Prevents console from closing immediately when debugging
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();
            }
        }

        public static bool IsHostedInServiceFabric => Environment.GetEnvironmentVariable("Fabric_ApplicationName") != null;

        /// <summary>
        /// Holds all configurations.
        /// Less ideal to use it as a global singleton, but this is a mock service after all
        /// </summary>
        public static IPrivacyConfigurationManager PartnerMockConfigurations;

        /// <summary>
        ///     Provides access to the family config for this service
        /// </summary>
        private class FamilyDecoratorConfig : IFamilyDecoratorConfig
        {
            /// <summary>
            ///     Gets the Uri to retrieve family JWKS
            /// </summary>
            public string FamilyJwks => PartnerMockConfigurations.PartnerMockConfiguration.FamilyJwksUri;
        }

        private static void SetupServiceDependencies(
            out ILogger logger,
            out DependencyManager dependencyResolver,
            out IPrivacyConfigurationManager configurationManager)
        {
            try
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
            catch (Exception e)
            {
                DualLogger.Instance.Error(nameof(Program), e, e.Message);
                throw;
            }
        }
    }
}
