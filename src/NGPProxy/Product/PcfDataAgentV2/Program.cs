// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgentV2
{
    using System;
    using System.Diagnostics;
    using System.Net;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Ms.Qos;

    internal static class Program
    {
        private const string OperationName = "NgpProxyPcfDataAgentV2";

        internal static void Main(string[] args)
        {
            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();

            SetupDependencies(out ICertificateProvider certificateProvider, out IPrivacyConfigurationManager configurationManager, out ILogger logger);

            HostDecorator hostSettings = new AppConfigDecorator();
            HostDecorator syntheticPcfReceiverDecorator = new PcfDataAgentV2Decorator(configurationManager.PcfDataAgentV2Config, certificateProvider, logger);
            HostDecorator removeConsoleLogger = new RemoveConsoleLoggerDecorator(configurationManager.PcfDataAgentV2Config.EnableConsoleLogging);

            try
            {
                SetupSllOperationName(OperationName);

                IHost serviceHost = HostFactory.CreatePipeline(
                    hostSettings,
                    syntheticPcfReceiverDecorator,
                    removeConsoleLogger);
                serviceHost.Execute();
            }
            catch (Exception ex)
            {
                logger.Error(nameof(Program), ex, ex.Message);
                throw;
            }
        }

        private static void SetupDependencies(out ICertificateProvider certificateProvider, out IPrivacyConfigurationManager configurationManager, out ILogger logger)
        {
            try
            {
                logger = DualLogger.Instance;
                DualLogger.AddTraceListener();

                Trace.TraceInformation($"Executing method: {nameof(SetupDependencies)}");
                
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

                var container = new UnityContainer();
                container.RegisterInstance<ILogger>(logger);

                IPrivacyConfigurationLoader configLoader = new PrivacyConfigurationLoader(logger);
                configurationManager = configLoader.CurrentConfiguration;
                certificateProvider = new CertificateProvider(logger);

                logger.Information(nameof(Program), "Dependencies initialized.");
            }
            catch (Exception e)
            {
                IfxTraceLogger.Instance.Error(nameof(Program), e, e.Message);
                throw;
            }
        }

        private static void SetupSllOperationName(string operationName)
        {
            Sll.Context.ChangeIncomingEvent(new IncomingApiEvent { baseData = new IncomingServiceRequest { operationName = operationName } });
        }
    }
}
