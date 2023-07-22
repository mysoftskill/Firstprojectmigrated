// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Host;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;

    class Program
    {
        /// <summary>
        ///     Privacy Worker schedules worker tasks for various privacy features (export dequeuer, delete feed archiver).
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();

            IPrivacyConfigurationManager configurationManager;
            ILogger logger;
            DependencyManager dependencyManager;
            SetupServiceDependencies(out configurationManager, out logger, out dependencyManager);

            HostDecorator hostSettings = new AppConfigDecorator();
            HostDecorator schedulerDecorator = new SchedulerDecorator(
                configurationManager,
                logger,
                dependencyManager.Container.Resolve<IAzureStorageProvider>(),
                dependencyManager.Container.Resolve<IExportStorageProvider>());

            //            HostDecorator nativeAssemblyLoaderDecorator = new NativeAssemblyLoaderDecorator();
            HostDecorator dequeuer = new DequeuerDecorator(dependencyManager);
            HostDecorator deleteExportArchivesDequeuerDecorator = new DeleteExportArchivesDequeuerDecorator(dependencyManager);
            HostDecorator removeConsoleLogger = new RemoveConsoleLoggerDecorator();

            // Startup pipeline components execute from left to right
            try
            {
                IHost serviceHost = HostFactory.CreatePipeline(
                    hostSettings,
                    dequeuer,
                    deleteExportArchivesDequeuerDecorator,
                    schedulerDecorator,
                    removeConsoleLogger);
                serviceHost.Execute();
            }
            catch (Exception ex)
            {
                Trace.Fail("unable to initialize decorators " + ex);
                throw;
            }

            Trace.TraceInformation("Worker ending");
        }

        private static void SetupServiceDependencies(
            out IPrivacyConfigurationManager configurationManager,
            out ILogger logger,
            out DependencyManager dependencyManager)
        {
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

            var container = new UnityContainer();

            container.RegisterInstance<ILogger>(IfxTraceLogger.Instance);
            Trace.Listeners.Add(IfxTraceLogger.Instance);
            logger = container.Resolve<ILogger>();

            IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);

            dependencyManager = new DependencyManager(container);
            dependencyManager.Initialize(configLoader);
            configurationManager = configLoader.CurrentConfiguration;
        }
    }
}
