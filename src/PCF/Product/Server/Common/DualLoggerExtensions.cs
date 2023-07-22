using Microsoft.PrivacyServices.Common.Azure;
using System;
using System.Globalization;

namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    public static class DualLoggerExtensions
    {
        public static void LogInformationForCommandLifeCycle(this DualLogger logger, string componentName, string message, params object[] args)
        {
            if(FlightingUtilities.IsEnabled(FlightingNames.PCFCommandLifeCycleLoggingEnabled))
            {
                logger.Information(componentName, message, args);
            }
        }

        public static void LogErrorForCommandLifeCycle(this DualLogger logger, string componentName, Exception ex, string message, params object[] args)
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.PCFCommandLifeCycleLoggingEnabled))
            {
                logger.Error(componentName, ex, message, args);
            }
        }

        public static void LogWarningForCommandLifeCycle(this DualLogger logger, string componentName, string message, params object[] args)
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.PCFCommandLifeCycleLoggingEnabled))
            {
                logger.Warning(componentName, message, args);
            }
        }
    }
}
