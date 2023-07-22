// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    
    using Microsoft.PrivacyServices.Common.Azure;

    public class Program
    {
        private const string WorkerName = "Data Action Runner";

        /// <summary>
        ///     Entry point for Data Action Runner
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
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

            IUnityContainer container = new UnityContainer();
            ILogger logger;

            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();

#if DEBUG
            TraceLogger.UseTraceWriteLineAlways = true;
            TraceLogger.UseSingleLineMessage = true;
            Trace.Listeners.Insert(0, new DefaultTraceListener());
#endif

            logger = this.SetupLogging(container);

            logger.Information(Program.WorkerName, "Starting...");

            try
            {
                IPrivacyConfigurationLoader config = this.SetupConfig(container, logger);
                IDataActionRunnerConfig agentConfig = config.CurrentConfiguration.DataActionRunnerConfig;
                DependencyManager resolver = this.SetupResolver(container);
                IHost host;

                resolver.Initialize(config);



                // pipeline components execute from left to right
                host = HostFactory.CreatePipeline(
                    new TaskSetManager(agentConfig, resolver, logger),
                    new RemoveConsoleLoggerDecorator(false));

                host.Execute();
            }
            catch (Exception e)
            {
                logger.Error(Program.WorkerName, e, "Startup failed");
                throw;
            }

            logger.Information(Program.WorkerName, "Terminating");

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
        /// <param name="logger">trace logger</param>
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
