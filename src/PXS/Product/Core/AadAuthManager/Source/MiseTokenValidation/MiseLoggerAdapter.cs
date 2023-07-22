// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.ServiceEssentials;
    using Microsoft.IdentityModel.Abstractions;
    using ILogger = PrivacyServices.Common.Azure.ILogger;

    public class MiseLoggerAdapter : IMiseLogger, IIdentityLogger
    {
        private readonly ILogger _customLogger = null;

        private static readonly string componentName = "MiseTokenValidationUtility";

        public MiseLoggerAdapter(ILogger customLogger)
        {
            _customLogger = customLogger;
        }

        public bool IsEnabled(LogSeverityLevel logSeverityLevel)
        {
            return ConvertToLogLevel(logSeverityLevel) >= 0;
        }

        private LogLevel ConvertToLogLevel(LogSeverityLevel severityLevel)
        {
            switch (severityLevel)
            {
                case LogSeverityLevel.Warning:
                    return LogLevel.Warning; 
                case LogSeverityLevel.Error:
                    return LogLevel.Error;
                case LogSeverityLevel.Critical:
                    return LogLevel.Critical;
                default:
                    return LogLevel.None;
            }
        }

        public void Log(string message, LogSeverityLevel severityLevel)
        {
            switch (severityLevel)
            {
                case LogSeverityLevel.Warning:
                    _customLogger.Warning(componentName, message);
                    break;

                case LogSeverityLevel.Error:
                    _customLogger.Error(componentName, message);
                    break;

                case LogSeverityLevel.Critical:
                    _customLogger.Error(componentName, message);
                    break;

                default:
                    break;
            }
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return ConvertToLogLevel(eventLogLevel) >= LogLevel.Warning;
        }

        public LogLevel ConvertToLogLevel(EventLogLevel eventLogLevel)
        {
            switch (eventLogLevel)
            {
                case EventLogLevel.LogAlways:
                    return LogLevel.Trace;
                case EventLogLevel.Verbose:
                    return LogLevel.Debug;
                case EventLogLevel.Informational:
                    return LogLevel.Information;
                case EventLogLevel.Warning:
                    return LogLevel.Warning;
                case EventLogLevel.Error:
                    return LogLevel.Error;
                case EventLogLevel.Critical:
                    return LogLevel.Critical;
                default:
                    return LogLevel.None;
            }
        }

        public void Log(LogEntry entry)
        {
            if (entry != null)
            {
                switch (entry.EventLogLevel)
                {
                    case EventLogLevel.Warning:
                        _customLogger.Warning(componentName, entry.Message);
                        break;

                    case EventLogLevel.Error:
                        _customLogger.Error(componentName, entry.Message);
                        break;

                    case EventLogLevel.Critical:
                        _customLogger.Error(componentName, entry.Message);
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
