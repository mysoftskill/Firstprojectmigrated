// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Logging
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Logs messages to ETW via the Instrumentation Framework (IFx)
    /// </summary>
    public class IfxLogger : TraceListener, ILogger
    {
        private const string ErrorMessageType = "ERROR";

        private const string InfoMessageType = "INFO";

        private const string VerboseMessageType = "VERBOSE";

        private const string WarningMessageType = "WARNING";

        private readonly IIfxEnvironment envSettings;

        /// <summary>
        ///     Initializes a new instance of the IfxLogger class
        /// </summary>
        /// <param name="envSettings">environment variables configuration</param>
        /// <param name="initializer">
        ///     the entire point of this parameter is to force Unity to create a new instance of IIfxInitializer in a
        ///     non-static context. It is intentionally not used
        /// </param>
        public IfxLogger(
            IIfxEnvironment envSettings,
            IIfxInitializer initializer)
        {
            this.envSettings = envSettings;
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="exception">exception to log</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Error(
            string componentName,
            Exception exception,
            string messageFormat,
            params object[] args)
        {
            this.WriteError(componentName, CreateFullMessage(exception, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Error(
            string componentName,
            string messageFormat,
            params object[] args)
        {
            this.WriteError(componentName, CreateFullMessage(null, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="exception">exception to log</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Information(
            string componentName,
            Exception exception,
            string messageFormat,
            params object[] args)
        {
            this.WriteInformational(componentName, CreateFullMessage(exception, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Information(
            string componentName,
            string messageFormat,
            params object[] args)
        {
            this.WriteInformational(componentName, CreateFullMessage(null, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        public void Log(
            IfxTracingLevel traceLevel,
            string componentName,
            string messageFormat,
            params object[] args)
        {
            switch (traceLevel)
            {
                case IfxTracingLevel.Error:
                    this.Error(componentName, messageFormat, args);
                    break;

                case IfxTracingLevel.Warning:
                    this.Warning(componentName, messageFormat, args);
                    break;

                case IfxTracingLevel.Informational:
                    this.Information(componentName, messageFormat, args);
                    break;

                case IfxTracingLevel.Verbose:
                    this.Verbose(componentName, messageFormat, args);
                    break;
            }
        }

        /// <summary>
        ///     <seealso cref="TraceListener.TraceEvent(TraceEventCache, string, TraceEventType, int, string)" />
        /// </summary>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (this.Filter != null &&
                !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    this.Error(source, message);
                    break;
                case TraceEventType.Warning:
                    this.Warning(source, message);
                    break;
                case TraceEventType.Verbose:
                    this.Verbose(source, message);
                    break;
                default:
                    this.Information(source, message);
                    break;
            }
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="exception">exception to log</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Verbose(
            string componentName,
            Exception exception,
            string messageFormat,
            params object[] args)
        {
            this.WriteVerbose(componentName, CreateFullMessage(exception, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Verbose(
            string componentName,
            string messageFormat,
            params object[] args)
        {
            this.WriteVerbose(componentName, CreateFullMessage(null, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="exception">exception to log</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Warning(
            string componentName,
            Exception exception,
            string messageFormat,
            params object[] args)
        {
            this.WriteWarning(componentName, CreateFullMessage(exception, messageFormat, args));
        }

        /// <summary>
        ///     Writes the specified message to ETW
        /// </summary>
        /// <param name="componentName">component name</param>
        /// <param name="messageFormat">message format string</param>
        /// <param name="args">message replacement args</param>
        public void Warning(
            string componentName,
            string messageFormat,
            params object[] args)
        {
            this.WriteWarning(componentName, CreateFullMessage(null, messageFormat, args));
        }

        /// <inheritdoc />
        public override void Write(string message)
        {
            this.WriteLine(message);
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            this.Information(nameof(IfxLogger), message);
        }

        /// <summary>
        ///     makes a full component given a component supplied by the caller (or null if no supplied component)
        /// </summary>
        /// <param name="component">component to use</param>
        /// <returns>resulting value</returns>
        private string MakeFullComponent(string component)
        {
            return component != null ? this.envSettings.ServiceName + "." + component : this.envSettings.ServiceName;
        }

        /// <summary>
        ///     Sends an informational message to IFx
        /// </summary>
        /// <param name="component">trace specific message component (or null to have no trace specific component)</param>
        /// <param name="message">message to send</param>
        private void WriteError(
            string component,
            string message)
        {
            FixCorrelationVector();

            IfxTracer.LogPropertyBag(
                IfxTracingLevel.Error,
                this.MakeFullComponent(component),
                ErrorMessageType,
                message);
        }

        /// <summary>
        ///     Sends an informational message to IFx
        /// </summary>
        /// <param name="component">trace specific message component (or null to have no trace specific component)</param>
        /// <param name="message">message to send</param>
        private void WriteInformational(
            string component,
            string message)
        {
            FixCorrelationVector();

            IfxTracer.LogPropertyBag(
                IfxTracingLevel.Informational,
                this.MakeFullComponent(component),
                InfoMessageType,
                message);
        }

        /// <summary>
        ///     Sends an informational message to IFx
        /// </summary>
        /// <param name="component">trace specific message component (or null to have no trace specific component)</param>
        /// <param name="message">message to send</param>
        private void WriteVerbose(
            string component,
            string message)
        {
            FixCorrelationVector();

            IfxTracer.LogPropertyBag(
                IfxTracingLevel.Verbose,
                this.MakeFullComponent(component),
                VerboseMessageType,
                message);
        }

        /// <summary>
        ///     Sends an informational message to IFx
        /// </summary>
        /// <param name="component">trace specific message component (or null to have no trace specific component)</param>
        /// <param name="message">message to send</param>
        private void WriteWarning(
            string component,
            string message)
        {
            FixCorrelationVector();

            IfxTracer.LogPropertyBag(
                IfxTracingLevel.Warning,
                this.MakeFullComponent(component),
                WarningMessageType,
                message);
        }

        /// <summary>
        ///     Creates a formatted log message
        /// </summary>
        /// <param name="exception">optional exception to be logged</param>
        /// <param name="message">raw log message (can be a format string)</param>
        /// <param name="args">replacement arguments if message is a format string</param>
        /// <returns>fully formatted log message</returns>
        private static string CreateFullMessage(
            Exception exception,
            string message,
            params object[] args)
        {
            var logMessageBuilder = new StringBuilder();

            if (args != null && args.Length > 0)
            {
                logMessageBuilder.AppendFormatInvariant(message, args).AppendLine();
            }
            else
            {
                logMessageBuilder.AppendLine(message);
            }

            if (exception != null)
            {
                logMessageBuilder.Append("Exception:\n" + exception).AppendLine();
            }

            return logMessageBuilder.ToString();
        }

        /// <summary>
        ///     IFx currently has a problem when you log a message after an await if the thread changes. The managed code will have
        ///     the correct correlation context. However, the logging is done by native code, and the correct context is NOT passed
        ///     to the native code. As a result, we have to get the managed context and then set it again, which will update both
        ///     the managed and native contexts.
        /// </summary>
        private static void FixCorrelationVector()
        {
            byte[] currentContext = CorrelationContext.Get();
            CorrelationContext.Set(currentContext);
        }
    }
}
