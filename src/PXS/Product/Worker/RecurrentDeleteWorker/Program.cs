// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker
{
    using System;
    using System.Diagnostics;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Practices.Unity;
    using Ms.Qos;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Host;

    internal static class Program
    {
        private const string OperationName = "RecurringDelete";

        private static IDependencyManager LoadDependencies()
        {
            try
            {
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

                var container = new UnityContainer();
                container.RegisterInstance<ILogger>(DualLogger.Instance);
                Trace.Listeners.Add(IfxTraceLogger.Instance);

                var logger = container.Resolve<ILogger>();
                IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);
                var dependencyManager = new DependencyManager(container, configLoader);
                IfxTraceLogger.Instance.Information(nameof(Program), "Dependency loaded.");
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

            HostDecorator recurringDeleteScheduleScanner = new RecurringDeleteHostDecorator(dependencies, DependencyManager.RecurrentDeleteScheduleScanner);
            HostDecorator recurringDeleteScheduleWorker = new RecurringDeleteHostDecorator(dependencies, DependencyManager.RecurrentDeleteScheduleWorker);
            HostDecorator preVerifierScanner = new RecurringDeleteHostDecorator(dependencies, DependencyManager.PreVerifierScanner);
            HostDecorator preVerifierWorker = new RecurringDeleteHostDecorator(dependencies, DependencyManager.PreVerifierWorker);

            HostDecorator removeConsoleLoggerDecorator = new RemoveConsoleLoggerDecorator(
                dependencies.Container.Resolve<IPrivacyConfigurationManager>().RecurringDeleteWorkerConfiguration.EnableConsoleLogging);

            try
            {
                SetupSllOperationName(OperationName);

                IHost serviceHost = HostFactory.CreatePipeline(
                    recurringDeleteScheduleScanner,
                    recurringDeleteScheduleWorker,
                    preVerifierScanner,
                    preVerifierWorker);
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
