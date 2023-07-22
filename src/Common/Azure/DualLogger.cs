//--------------------------------------------------------------------------------
// <copyright file="DualLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// A temporary logger to write to both Geneva and trace. Will remove later.
    /// </summary>
    public class DualLogger : ILogger
    {
        private readonly ILogger consoleLogger;
        private readonly IfxTraceLogger genevaLogger;

        private static DualLogger instance;

        /// <summary>
        ///     Gets the <see cref="DualLogger" /> instance.
        /// </summary>
        public static DualLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    DualLogger local = new DualLogger();
                    Interlocked.CompareExchange(ref instance, local, null);
                }

                return instance;
            }
        }

        /// <summary>
        /// Adds a trace listener for the <see cref="DualLogger"/>
        /// </summary>
        /// <param name="listenerName"></param>
        public static void AddTraceListener()
        {
            // Insert the geneva logger if it's not already there
            if (Trace.Listeners.IndexOf(DualLogger.Instance.genevaLogger) == -1)
            {
                Trace.Listeners.Insert(0, DualLogger.Instance.genevaLogger);
            }
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="DualLogger" /> class from being created.
        /// </summary>
        private DualLogger()
        {
            this.consoleLogger = new ConsoleLogger();
            this.genevaLogger = IfxTraceLogger.Instance;
        }

        /// <summary>
        /// Emits a message to our log according to the specified trace level.
        /// </summary>
        /// <param name="traceLevel">The trace level.</param>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        public void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args)
        {
            this.consoleLogger.Log(traceLevel, componentName, message, args);
            this.genevaLogger.Log(traceLevel, componentName, message, args);
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
            this.consoleLogger.Error(componentName, exception, message, args);
            this.genevaLogger.Error(componentName, exception, message, args);
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
            this.consoleLogger.Warning(componentName, exception, message, args);
            this.genevaLogger.Warning(componentName, exception, message, args);
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
            this.consoleLogger.Information(componentName, exception, message, args);
            this.genevaLogger.Information(componentName, exception, message, args);
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
            this.consoleLogger.Verbose(componentName, exception, message, args);
            this.genevaLogger.Verbose(componentName, exception, message, args);
        }
    }
}
