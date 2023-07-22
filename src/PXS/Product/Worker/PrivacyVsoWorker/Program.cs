namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Host;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    
    using Ms.Qos;

    using Microsoft.PrivacyServices.Common.Azure;

    internal static class Program
    {
        private const string OperationName = "PrivacyVsoWorker";

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

            HostDecorator itemProcessor = new ItemProcessorDecorator(dependencies);

            HostDecorator removeConsoleLoggerDecorator = new RemoveConsoleLoggerDecorator(
                dependencies.Container.Resolve<IPrivacyConfigurationManager>().PrivacyVsoWorkerConfiguration.EnableConsoleLogging);

            try
            {
                if (dependencies.Container.Resolve<IPrivacyConfigurationManager>().PrivacyVsoWorkerConfiguration.EnableWorker)
                {
                    SetupSllOperationName(OperationName);
                    IHost serviceHost = HostFactory.CreatePipeline(
                        itemProcessor,
                        removeConsoleLoggerDecorator);

                    serviceHost.Execute();
                }
                else
                {
                    IfxTraceLogger.Instance.Information(nameof(Program), "PrivacyVsoWorker is disabled.");
                }
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
