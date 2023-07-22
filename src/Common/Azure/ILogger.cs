//--------------------------------------------------------------------------------
// <copyright file="ILogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// Logger interface.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Emits a message to our log according to the specified trace level.
        /// </summary>
        /// <param name="traceLevel">The trace level.</param>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args);

        /// <summary>
        /// Emit an error message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Error(string componentName, string message, params object[] args);

        /// <summary>
        /// Emit an error message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Error(string componentName, Exception exception, string message, params object[] args);

        /// <summary>
        /// Emit a warning message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Warning(string componentName, string message, params object[] args);

        /// <summary>
        /// Emit a warning message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Warning(string componentName, Exception exception, string message, params object[] args);

        /// <summary>
        /// Emit an informational message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Information(string componentName, string message, params object[] args);

        /// <summary>
        /// Emit an informational message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Information(string componentName, Exception exception, string message, params object[] args);

        /// <summary>
        /// Emit an verbose message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Verbose(string componentName, string message, params object[] args);

        /// <summary>
        /// Emit an verbose message to our log.
        /// </summary>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        void Verbose(string componentName, Exception exception, string message, params object[] args);
    }
}