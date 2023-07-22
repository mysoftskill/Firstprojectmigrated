//--------------------------------------------------------------------------------
// <copyright file="TraceLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Oss.Membership.CommonCore.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Thin wrapper around Trace logging class. It serves two purposes: (1) implements the ILogger interface to
    /// to keep uniform logging across our code and (2) formats the log message with component name, exception etc.
    /// </summary>
    public class TraceLogger : ILogger
    {
        private static TraceLogger instance;
        private static TraceSwitch traceSwitch = new TraceSwitch("traceSwitch", "Switch in app.config file");

        /// <summary>
        /// Sets the trace switch.
        /// </summary>
        public static TraceSwitch TraceSwitch
        {
            get => TraceLogger.traceSwitch; 
            set => TraceLogger.traceSwitch = value; 
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use single line output message
        /// </summary>
        public static bool UseSingleLineMessage { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to use single line output message
        /// </summary>
        public static bool UseTraceWriteLineAlways { get; set; } = false;

        /// <summary>
        /// Gets the <see cref="TraceLogger"/> instance.
        /// </summary>
        public static TraceLogger Instance
        {
            get
            {
                if (TraceLogger.instance == null)
                {
                    TraceLogger local = new TraceLogger();
                    Interlocked.CompareExchange(ref TraceLogger.instance, local, null);
                }

                return TraceLogger.instance;
            }
        }

        /// <summary>
        /// Emits a message to our log according to the specified trace level.
        /// </summary>
        /// <param name="traceLevel">The trace level.</param>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        public void Log(TraceLevel traceLevel, string componentName, string message, params object[] args)
        {
            switch (traceLevel)
            {
                case TraceLevel.Error:
                    this.Error(componentName, message, args);
                    break;
                case TraceLevel.Warning:
                    this.Warning(componentName, message, args);
                    break;
                case TraceLevel.Info:
                    this.Information(componentName, message, args);
                    break;
                case TraceLevel.Verbose:
                    this.Verbose(componentName, message, args);
                    break;
                case TraceLevel.Off:
                default:
                    return;
            }
        }

        public void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args)
        {
            switch (traceLevel)
            {
                case IfxTracingLevel.Critical:
                case IfxTracingLevel.Error:
                    this.Error(componentName, message, args);
                    break;
                case IfxTracingLevel.Warning:
                    this.Warning(componentName, message, args);
                    break;
                case IfxTracingLevel.Verbose:
                    this.Verbose(componentName, message, args);
                    break;
                case IfxTracingLevel.Informational:
                default:
                    this.Information(componentName, message, args);
                    break;
            }
        }

        /// <summary>
        /// Log error.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Error(string componentName, string message, params object[] args)
        {
            this.Error(componentName, null, message, args);
        }

        /// <summary>
        /// Log error.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Error(string componentName, Exception exception, string message, params object[] args)
        {
            if (TraceLogger.TraceSwitch.TraceError)
            {
                string logMessage = TraceLogger.CreateLogMessage(componentName, exception, message, args);
                if (TraceLogger.UseTraceWriteLineAlways)
                {
                    Trace.WriteLine(logMessage);
                }
                else
                {
                    Trace.TraceError(logMessage);
                }
            }
        }

        /// <summary>
        /// Log warning.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Warning(string componentName, string message, params object[] args)
        {
            this.Warning(componentName, null, message, args);
        }

        /// <summary>
        /// Log warning.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Warning(string componentName, Exception exception, string message, params object[] args)
        {
            if (TraceLogger.TraceSwitch.TraceWarning)
            {
                string logMessage = TraceLogger.CreateLogMessage(componentName, exception, message, args);
                if (TraceLogger.UseTraceWriteLineAlways)
                {
                    Trace.WriteLine(logMessage);
                }
                else
                {
                    Trace.TraceWarning(logMessage);
                }
            }
        }

        /// <summary>
        /// Log information.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Information(string componentName, string message, params object[] args)
        {
            this.Information(componentName, null, message, args);
        }

        /// <summary>
        /// Log information.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Information(string componentName, Exception exception, string message, params object[] args)
        {
            if (TraceLogger.TraceSwitch.TraceInfo)
            {
                string logMessage = TraceLogger.CreateLogMessage(componentName, exception, message, args);
                if (TraceLogger.UseTraceWriteLineAlways)
                {
                    Trace.WriteLine(logMessage);
                }
                else
                {
                    Trace.TraceInformation(logMessage);
                }
            }
        }

        /// <summary>
        /// Log verbose.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Verbose(string componentName, string message, params object[] args)
        {
            this.Verbose(componentName, null, message, args);
        }

        /// <summary>
        /// Log verbose.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Verbose(string componentName, Exception exception, string message, params object[] args)
        {
            if (TraceLogger.TraceSwitch.TraceVerbose)
            {
                string logMessage = TraceLogger.CreateLogMessage(componentName, exception, message, args);
                Trace.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Creates a formatted log message.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        /// <returns>The fully formatted log message</returns>
        private static string CreateLogMessage(string componentName, Exception exception, string message, params object[] args)
        {
            if (TraceLogger.UseSingleLineMessage)
            {
                string resultMessage = componentName + " " + (args.Length == 0 ? message : string.Format(message, args)); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
                return exception == null ? resultMessage : resultMessage + ": " + exception.ToString();
            }

            StringBuilder logMessageBuilder = new StringBuilder();

            // ComponentName.
            logMessageBuilder.AppendFormat(CultureInfo.InvariantCulture, "ComponentName: [{0}]", componentName).AppendLine();

            // Message.
            if (args != null && args.Length > 0)
            {
                logMessageBuilder.AppendFormat(CultureInfo.InvariantCulture, message, args).AppendLine(); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
            }
            else
            {
                logMessageBuilder.AppendLine(message);
            }

            // Exception.
            if (exception != null)
            {
                logMessageBuilder.AppendFormat(CultureInfo.InvariantCulture, "Exception : [{0}]", exception).AppendLine();
            }
            
            return logMessageBuilder.ToString();
        }
    }
}
