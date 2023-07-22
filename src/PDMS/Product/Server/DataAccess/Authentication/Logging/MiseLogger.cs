using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.Identity.ServiceEssentials;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.PrivacyServices.Common.Azure;
using System;

namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.Logging
{
    /// <summary>
    /// The default implementation of <see cref="IMiseLogger"/> and <see cref="IIdentityLogger"/> interfaces to provide a wrapper around <see cref="ILogger"/> instance to funnel .NET Identity logs to it.
    /// </summary>
    public sealed class MiseLogger : IMiseLogger, IIdentityLogger
    {
        private readonly ILogger _logger;
        private static readonly string ComponentName = "AzureActiveDirectoryProvider-MISE";

        private static readonly MiseLogger miseLoggerInstance = new MiseLogger(DualLogger.Instance);

        /// <summary>
        /// Instantiates <see cref="MiseLogger"/> using <paramref name="logger"/>
        /// </summary>
        /// <param name="logger"></param>
        private MiseLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogSeverityLevel logSeverityLevel)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log(string message, LogSeverityLevel severityLevel)
        {
            switch (severityLevel)
            {
                case LogSeverityLevel.Trace:
                case LogSeverityLevel.Information:
                    _logger.Log(IfxTracingLevel.Informational, ComponentName, message);
                    break;
                case LogSeverityLevel.Debug:
                    _logger.Log(IfxTracingLevel.Verbose, ComponentName, message);
                    break;
                case LogSeverityLevel.Warning:
                    _logger.Log(IfxTracingLevel.Warning, ComponentName, message);
                    break;
                case LogSeverityLevel.Error:
                    _logger.Log(IfxTracingLevel.Error, ComponentName, message);
                    break;
                case LogSeverityLevel.Critical:
                    _logger.Log(IfxTracingLevel.Critical, ComponentName, message);
                    break;
                default:
                    break;
            }
        }

        private static IfxTracingLevel ConvertToLogLevel(LogSeverityLevel severityLevel)
        {
            switch(severityLevel)
            {
                case LogSeverityLevel.Trace:
                case LogSeverityLevel.Information:
                    return IfxTracingLevel.Informational;
                case LogSeverityLevel.Debug:
                    return IfxTracingLevel.Verbose;
                case LogSeverityLevel.Warning:
                    return IfxTracingLevel.Warning;
                case LogSeverityLevel.Error:
                    return IfxTracingLevel.Error;
                case LogSeverityLevel.Critical:
                    return IfxTracingLevel.Critical;
                default:
                    return IfxTracingLevel.Informational;

            }
        }

        /// <inheritdoc/>
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log(LogEntry entry)
        {
            if (entry != null)
            {
                switch (entry.EventLogLevel)
                {
                    case EventLogLevel.Critical:
                        _logger.Log(IfxTracingLevel.Critical, ComponentName, entry.Message);
                        break;

                    case EventLogLevel.Error:
                        _logger.Log(IfxTracingLevel.Error, ComponentName, entry.Message);
                        break;

                    case EventLogLevel.Warning:
                        _logger.Log(IfxTracingLevel.Warning, ComponentName, entry.Message);
                        break;

                    case EventLogLevel.Informational:
                        _logger.Log(IfxTracingLevel.Informational, ComponentName, entry.Message);
                        break;

                    case EventLogLevel.Verbose:
                        _logger.Log(IfxTracingLevel.Verbose, ComponentName, entry.Message);
                        break;

                    case EventLogLevel.LogAlways:
                        _logger.Log(IfxTracingLevel.Verbose, ComponentName, entry.Message);
                        break;

                    default:
                        break;
                }
            }
        }

        private static IfxTracingLevel ConvertToLogLevel(EventLogLevel eventLogLevel)
        {
            switch (eventLogLevel)
            {
                case EventLogLevel.LogAlways:
                    return IfxTracingLevel.Verbose;
                case EventLogLevel.Verbose:
                    return IfxTracingLevel.Verbose;
                case EventLogLevel.Informational:
                    return IfxTracingLevel.Informational;
                case EventLogLevel.Warning:
                    return IfxTracingLevel.Warning;
                case EventLogLevel.Error:
                    return IfxTracingLevel.Error;
                case EventLogLevel.Critical:
                    return IfxTracingLevel.Critical;
                default:
                    return IfxTracingLevel.Verbose;
            }

        }
        public static MiseLogger Instance
        {
            get
            {
                return miseLoggerInstance;
            }
        }
    }
}
