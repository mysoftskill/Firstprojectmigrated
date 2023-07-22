//--------------------------------------------------------------------------------
// <copyright file="ConsoleLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Globalization;
    using System.Text;

    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// Implements ILogger using ConsoleWrites. 
    /// This is intended only for command line tools.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Emits a message to our log according to the specified trace level.
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
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            Console.WriteLine(logMessage);
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
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            Console.WriteLine(logMessage);
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
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            Console.WriteLine(logMessage);
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
            string logMessage = CreateLogMessage(componentName, exception, message, args);
            Console.WriteLine(logMessage);
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
