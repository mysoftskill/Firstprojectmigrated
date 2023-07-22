//--------------------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;

    /// <summary>
    /// Miscellaneous <see cref="ILogger"/> extension methods.
    /// </summary>
    public static class LoggingHelper
    {
        /// <summary>
        /// The method-entered message-format.
        /// </summary>
        private const string MethodEnterFormat = "Method Enter: [{0}]";

        /// <summary>
        /// The method-action with message.
        /// </summary>
        private const string MethodActionWithMessageFormat = "{0}. {1}";

        /// <summary>
        /// The method-exited message-format.
        /// </summary>
        private const string MethodExitFormat = "Method Exit: [{0}]";

        /// <summary>
        /// The method-exited-with-an-exception message-format.
        /// </summary>
        private const string MethodExceptionFormat = "Method {0} threw exception. Exception message: {1}";

        /// <summary>
        /// The method-exited-successfully (with a response) message-format.
        /// </summary>
        private const string MethodSuccessFormat = "Method {0} exit successfully. Respone: {1}";

        /// <summary>
        /// The method-exited-successfully message-format.
        /// </summary>
        private const string VoidMethodSuccessFormat = "Method {0} exit succesfully.";

        /// <summary>
        /// Log method-entered.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        public static void MethodEnter(this ILogger logger, string componentName, string methodName)
        {
            logger.Information(componentName, MethodEnterFormat, methodName);
        }

        /// <summary>
        /// Log method-entered with message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="message">The message, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        public static void MethodEnter(this ILogger logger, string componentName, string methodName, string message, params object[] args)
        {
            string method = string.Format(MethodEnterFormat, methodName);
            string info = string.Format(message, args);
            logger.Information(componentName, MethodActionWithMessageFormat, method, info);
        }

        /// <summary>
        /// Log method-exited.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        public static void MethodExit(this ILogger logger, string componentName, string methodName)
        {
            logger.Information(componentName, MethodExitFormat, methodName);
        }

        /// <summary>
        /// Log method-exited successfully.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="response">The method response.</param>
        public static void MethodSuccess(this ILogger logger, string componentName, string methodName, object response)
        {
            logger.Information(componentName, MethodSuccessFormat, methodName, response);
        }

        /// <summary>
        /// Log method-exited successfully.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        public static void MethodSuccess(this ILogger logger, string componentName, string methodName)
        {
            logger.Information(componentName, VoidMethodSuccessFormat, methodName);
        }

        /// <summary>
        /// Log method-exited with an exception.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="ex">The method exception.</param>
        public static void MethodException(this ILogger logger, string componentName, string methodName, Exception ex)
        {
            logger.Error(componentName, MethodExceptionFormat, methodName, ex);
        }

        /// <summary>
        /// Log method-exited with a warning.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="componentName">The component name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="message">The method warning message.</param>
        public static void MethodWarning(this ILogger logger, string componentName, string methodName, string message)
        {
            logger.Warning(componentName, MethodExceptionFormat, methodName, message);
        }
    }
}
