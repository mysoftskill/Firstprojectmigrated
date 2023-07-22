// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Metrics
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Initializes the metric default dimensions
    /// </summary>
    public class IfxMetricInitializer : IIfxMetricInitializer
    {
        /// <summary>
        ///     Initializes a new instance of the IfxMetricInitializer class
        /// </summary>
        /// <param name="logger">Used to log problems with metric registration</param>
        /// <param name="environment">Contains information about the environment (host)</param>
        public IfxMetricInitializer(
            ILogger logger,
            IIfxEnvironment environment)
        {
            IfxMetricInitializer.InitializeDefaultAccount(environment, logger);
            IfxMetricInitializer.InitializeDimensions(environment, logger);
        }

        /// <summary>
        ///     Initializes the default account that IFX will use for the monitoring account
        /// </summary>
        /// <param name="env">Used for the service name</param>
        /// <param name="logger">Used to log errors</param>
        private static void InitializeDefaultAccount(
            IIfxEnvironment env,
            ILogger logger)
        {
            ErrorContext errCtx = new ErrorContext();

            // Set default monitoring account
            if (DefaultConfiguration.SetDefaulMonitoringAccount(ref errCtx, env.MonitoringAccount) == false)
            {
                logger.Warning(
                    "Failed to set the default monitoring account for Service:'{0}', error code is '{1:X}'.  Message:'{2}'.",
                    env.ServiceName,
                    errCtx.ErrorCode,
                    errCtx.ErrorMessage);
            }
        }

        /// <summary>
        ///     Initialize the default dimensions that will be emitted with metrics
        /// </summary>
        /// <param name="env">Contains information like service name, role, etc</param>
        /// <param name="logger">Used to log errors with metric initialization</param>
        private static void InitializeDimensions(
            IIfxEnvironment env,
            ILogger logger)
        {
            ErrorContext errCtx = new ErrorContext();
            List<string> defDimNames = new List<string> { "Service", "Datacenter", "Role", "RoleInstance" };
            List<string> defDimVals = new List<string> { env.ServiceName, env.Datacenter, env.Role, Environment.MachineName };
            bool result;

            if (string.IsNullOrWhiteSpace(env.Cloud) == false)
            {
                defDimNames.Add("Cloud");
                defDimVals.Add(env.Cloud);
            }

            result = DefaultConfiguration.SetDefaultDimensionNamesValues(
                ref errCtx, 
                (uint)defDimVals.Count,  
                defDimNames.ToArray(), 
                defDimVals.ToArray());

            if (result == false)
            {
                logger.Warning(
                    typeof(IfxMetricInitializer).Name,
                    $"Error setting default dimension values for service {env.ServiceName}: {errCtx.ErrorMessage}");
            }
            else
            {
                logger.Verbose(
                    typeof(IfxMetricInitializer).Name,
                    "Initialized metric factory for Service " + env.ServiceName);
            }
        }
    }
}