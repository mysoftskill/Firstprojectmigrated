// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    
    using Microsoft.PrivacyServices.Common.Azure;

    public class Program
    {
        private const string WorkerName = "Cosmos Export Package Processor Worker";

        /// <summary>
        ///     Entry point for Cosmos Export worker
        /// </summary>
        /// <param name="args">program arguments</param>
        public static void Main(string[] args)
        {
            new Program().Run(args);
        }

        /// <summary>
        ///     Entry point for Cosmos Export worker
        /// </summary>
        /// <param name="args">program arguments</param>
        public void Run(string[] args)
        {
            // Set on devbox only for debugging
            //Environment.SetEnvironmentVariable("AzureServicesAuthConnectionString", "RunAs=App;AppId=705363a0-5817-47fb-ba32-59f47ce80bb7;TenantId=f8cdef31-a31e-4b4a-93e4-5f571e91255a;CertificateSubjectName=CN=aad-ame2.ppe.dpp.microsoft.com;CertificateStoreLocation=LocalMachine");

            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

            IUnityContainer container = new UnityContainer();

            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();

            ILogger logger = this.SetupLogging(container);

            logger.Information(Program.WorkerName, "Cosmos export worker startup");

            try
            {
                IPrivacyConfigurationLoader config = this.SetupConfig(container, logger);
                ICosmosExportAgentConfig agentConfig = config.CurrentConfiguration.CosmosExportAgentConfig;
                DependencyManager resolver = this.SetupResolver(container);
                IHost host;

                resolver.Initialize(config);

                // pipeline components execute from left to right (
                host = HostFactory.CreatePipeline(
                    new ServicePointSetup(agentConfig, logger),
                    new TaskSetManager(agentConfig, resolver, logger),
                    new RemoveConsoleLoggerDecorator());

                host.Execute();
            }
            catch (Exception e)
            {
                logger.Error(Program.WorkerName, e, "Failed to start cosmos export worker");
                throw;
            }

            logger.Information(Program.WorkerName, "Cosmos export worker terminating");

            Trace.Flush();
        }
       
        /// <summary>Sets up trace logging</summary>
        /// <param name="container">unity container in which to register objects</param>
        /// <returns>resulting value</returns>
        private ILogger SetupLogging(IUnityContainer container)
        {
            DualLogger logger = DualLogger.Instance;
            DualLogger.AddTraceListener();
            container.RegisterInstance<ILogger>(logger);
            return logger;
        }

        /// <summary>Sets up the configuration manager</summary>
        /// <param name="container">unity container in which to register objects</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <returns>resulting value</returns>
        private IPrivacyConfigurationLoader SetupConfig(
            IUnityContainer container,
            ILogger logger)
        {
            IPrivacyConfigurationLoader loader = new PrivacyConfigurationLoader(logger);
            container.RegisterInstance(loader);
            return loader;
        }

        /// <summary>
        ///     Sets up the type resolver
        /// </summary>
        /// <param name="container">unity container in which to register objects</param>
        private DependencyManager SetupResolver(IUnityContainer container)
        {
            return new DependencyManager(container);
        }
    }
}
