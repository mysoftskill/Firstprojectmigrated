// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     Thin wrapper around IfxTracer logging class. It serves two purposes: (1) implements the ILogger interface
    ///     to keep uniform logging across our code and (2) formats the log message with component name, exception etc.
    /// </summary>
    public class IfxTraceLogger : TraceListener, ILogger
    {
        private static IfxTraceLogger instance;

        /// <summary>
        ///     Gets the <see cref="IfxTraceLogger" /> instance.
        /// </summary>
        public static IfxTraceLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    IfxTraceLogger local = new IfxTraceLogger() { Name = "GenevaTraceListener" };
                    Interlocked.CompareExchange(ref instance, local, null);
                }

                return instance;
            }
        }

        /// <summary>
        ///     Tag id prefix.
        /// </summary>
        public static string TagIdPrefix { get; set; } = TraceTagPrefixes.ADGCS.ToString();

        /// <summary>
        ///     Gets or sets a value indicating whether to use single line output message.
        /// </summary>
        public static bool UseSingleLineMessage { get; set; } = false;

        /// <summary>
        ///     Get corresponding Ifx tracing level for diagnostic tracing event level.
        /// </summary>
        /// <param name="eventLevel">Diagnostic tracing event level.</param>
        /// <returns>Corresponding Ifx tracing level.</returns>
        public static IfxTracingLevel GetIfxTracingLevel(EventLevel eventLevel)
        {
            switch (eventLevel)
            {
                case EventLevel.Verbose:
                    return IfxTracingLevel.Verbose;
                case EventLevel.Informational:
                    return IfxTracingLevel.Informational;
                case EventLevel.Warning:
                    return IfxTracingLevel.Warning;
                case EventLevel.Error:
                case EventLevel.Critical:
                    return IfxTracingLevel.Error;
                default:
                    return IfxTracingLevel.Informational;
            }
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="IfxTraceLogger" /> class from being created.
        /// </summary>
        private IfxTraceLogger()
        {
        }

        /// <summary>
        ///     Log error.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Error(string componentName, string message, params object[] args)
        {
            this.Error(componentName, null, message, args);
        }

        /// <summary>
        ///     Log error.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Error(string componentName, Exception exception, string message, params object[] args)
        {
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            IfxTracer.LogMessage(IfxTracingLevel.Error, $"{TagIdPrefix}_Error", logMessage);
        }

        /// <summary>
        ///     Log information.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Information(string componentName, string message, params object[] args)
        {
            this.Information(componentName, null, message, args);
        }

        /// <summary>
        ///     Log information.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Information(string componentName, Exception exception, string message, params object[] args)
        {
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            IfxTracer.LogMessage(IfxTracingLevel.Informational, $"{TagIdPrefix}_Informational", logMessage);
        }

        /// <summary>
        ///     Emits a message to our log according to the specified trace level.
        /// </summary>
        /// <param name="traceLevel">The trace level.</param>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        public void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args)
        {
            switch (traceLevel)
            {
                case IfxTracingLevel.Error:
                    this.Error(componentName, message, args);
                    break;
                case IfxTracingLevel.Warning:
                    this.Warning(componentName, message, args);
                    break;
                case IfxTracingLevel.Informational:
                    this.Information(componentName, message, args);
                    break;
                case IfxTracingLevel.Verbose:
                    this.Verbose(componentName, message, args);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        ///     Log verbose.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Verbose(string componentName, string message, params object[] args)
        {
            this.Verbose(componentName, null, message, args);
        }

        /// <summary>
        ///     Log verbose.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Verbose(string componentName, Exception exception, string message, params object[] args)
        {
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            IfxTracer.LogMessage(IfxTracingLevel.Verbose, $"{TagIdPrefix}_Verbose", logMessage);
        }

        /// <summary>
        ///     Log warning.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Warning(string componentName, string message, params object[] args)
        {
            this.Warning(componentName, null, message, args);
        }

        /// <summary>
        ///     Log warning.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        public void Warning(string componentName, Exception exception, string message, params object[] args)
        {
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            IfxTracer.LogMessage(IfxTracingLevel.Warning, $"{TagIdPrefix}_Warning", logMessage);
        }

        /// <summary>
        ///     Creates a formatted log message.
        /// </summary>
        /// <param name="componentName">The log source component.</param>
        /// <param name="exception">Exception to be logged, if any.</param>
        /// <param name="message">The raw log message.</param>
        /// <param name="args">Message arguments, if it is a format string.</param>
        /// <returns>The fully formatted log message.</returns>
        private static string CreateLogMessage(string componentName, Exception exception, string message, params object[] args)
        {
            if (UseSingleLineMessage)
            {
                string resultMessage = componentName + " " + (args.Length == 0 ? message : string.Format(message, args)); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
                return exception == null ? resultMessage : resultMessage + ": " + exception;
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

        /// <inheritdoc />
        public override void Write(string message)
        {
            this.WriteLine(message);
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            this.Information(nameof(IfxTraceLogger), message);
        }
    }
}
