// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.Host;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    
    using Ms.Qos;

    using Microsoft.PrivacyServices.Common.Azure;

    internal static class Program
    {
        private const string OperationName = "AadAccountClose";

        private static IDependencyManager LoadDependencies()
        {
            try
            {
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

                var container = new UnityContainer();
                container.RegisterInstance<ILogger>(IfxTraceLogger.Instance);
                Trace.Listeners.Add(IfxTraceLogger.Instance);

                var logger = container.Resolve<ILogger>();
                IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);
                var dependencyManager = new DependencyManager(container, configLoader);
                return dependencyManager;
            }
            catch (Exception e)
            {
                IfxTraceLogger.Instance.Error(nameof(Program), e, e.Message);
                throw;
            }
        }

        internal static void Main()
        {
            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();

            IDependencyManager dependencies = LoadDependencies();

            HostDecorator aadQueueProcessor = new AadQueueProcessorDecorator(dependencies);
            HostDecorator aadEventHubProcessorDecorator = new AadEventHubProcessorDecorator(dependencies);
            HostDecorator removeConsoleLoggerDecorator = new RemoveConsoleLoggerDecorator(
                dependencies.Container.Resolve<IPrivacyConfigurationManager>().AadAccountCloseWorkerConfiguration.EnableConsoleLogging);

            try
            {
                SetupSllOperationName(OperationName);
                IHost serviceHost = HostFactory.CreatePipeline(
                    aadQueueProcessor,
                    aadEventHubProcessorDecorator,
                    removeConsoleLoggerDecorator);
                serviceHost.Execute();
            }
            catch (Exception ex)
            {
                IfxTraceLogger.Instance.Error(nameof(Program), ex, ex.Message);
                throw;
            }
        }

        private static void SetupSllOperationName(string operationName)
        {
            Sll.Context.ChangeIncomingEvent(new IncomingApiEvent { baseData = new IncomingServiceRequest { operationName = operationName } });
        }
    }
}
