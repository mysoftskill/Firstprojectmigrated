// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.MsaAgeOutFakeCommandWorker
{
    using System;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    
    using Microsoft.PrivacyServices.Common.Azure;

    class Program
    {
        static void Main(string[] args)
        {
            DualLogger logger = null;

            try
            {
                ResetTraceDecorator.ResetTraceListeners();

                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);

                logger = DualLogger.Instance;
                DualLogger.AddTraceListener();

                IPrivacyConfigurationManager configurationManager = new PrivacyConfigurationLoader(logger).CurrentConfiguration;

                if (configurationManager.EnvironmentConfiguration == null || configurationManager.EnvironmentConfiguration.EnvironmentType == EnvironmentType.Prod)
                {
                    logger.Error(nameof(Program), "This executable is NOT intended for production use. exiting...");
                    return;
                }

                IHost serviceHost = HostFactory.CreatePipeline(
                    new MsaAgeOutQueueProcessorDecorator(logger, configurationManager),
                    new RemoveConsoleLoggerDecorator(configurationManager?.MsaAgeOutFakeCommandWorkerConfiguration?.EnableConsoleLogging ?? false));

                serviceHost.Execute();
            }
            catch (Exception ex)
            {
                logger?.Error(nameof(Program), ex, ex.ToString());
                throw;
            }
        }
    }
}
