// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Common
{
    using Microsoft.Membership.MemberServices.Configuration;

    using Cloud = Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     This class handles initializing IFX so that we can emit logs, operations, metrics, etc.
    /// </summary>
    public class IfxInitializer : IIfxInitializer
    {
        /// <summary>
        ///     Initializes a new instance of the IfxEventSession class
        /// </summary>
        /// <param name="envSettings">The environment variables configuration</param>
        public IfxInitializer(IIfxEnvironment envSettings)
        {
            this.InitializeIfx(envSettings);
        }

        /// <summary>
        ///     Initialize non dev environment.
        /// </summary>
        /// <param name="envSettings">Environment settings.</param>
        /// <param name="logDirectory">Local log directory.</param>
        protected virtual void NonDevEnvInitialize(IIfxEnvironment envSettings, string logDirectory)
        {
            // for one box testing using ifxconsumer with the following command line:
            //  IfxConsumer.exe -environment "PfxDev,Dev,dev"
            var instrumentationSpecification = new Cloud.InstrumentationSpecification()
            {
                TraceDirectory = logDirectory
            };

            Cloud.IfxInitializer.IfxInitialize("PfxDev", "Dev", "dev", instrumentationSpecification);
        }

        /// <summary>
        ///     Initialize IFX itself, which is required before calling other IFX methods
        /// </summary>
        /// <param name="envSettings">environment variables configuration</param>
        private void InitializeIfx(IIfxEnvironment envSettings)
        {
            Cloud.IfxInitializer.IfxInitialize(envSettings.MonitoringAgentSessionName,
                new Cloud.InstrumentationSpecification()
                {
                    EmitIfxMetricsEvents = true,
                    MonitoringAccountName = envSettings.MonitoringAccount
                });
        }
    }
}
