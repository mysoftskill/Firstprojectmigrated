// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     base contract for context classes
    /// </summary>
    public interface IContext : IDisposable
    {
        /// <summary>
        ///     Gets a tag indicating the current processing state 
        /// </summary>
        string Tag { get; }

        /// <summary>
        ///     Gets a value indicating whether this errors have been logged to this context
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        ///     Called by an action when it starts parsing
        /// </summary>
        /// <param name="type">action type</param>
        /// <param name="tag">tag of action being started</param>
        void OnActionStart(
            ActionType type,
            string tag);

        /// <summary>
        ///     Called by an action when it completes parsing
        /// </summary>
        void OnActionEnd();

        /// <summary>
        ///     Adds a message to the list of messages to be added before any reported parse error
        /// </summary>
        /// <param name="messageGenerator">message to add</param>
        /// <remarks>
        ///     this allows a parent action to set a conditional context setting message in the event one of its child actions
        ///      reports a parse error.  If no error is reported, this message is not emitted
        /// </remarks>
        void PushErrorIntroMessage(Func<string> messageGenerator);

        /// <summary>
        ///     Clears the last error intro message
        /// </summary>
        void PopErrorIntroMessage();

        /// <summary>
        ///     Logs an exception to the context stream
        /// </summary>
        /// <param name="exception">exception to log</param>
        /// <param name="message">message to log with the exception</param>
        void LogError(
            Exception exception,
            string message);

        /// <summary>
        ///     Logs an error message to the context stream
        /// </summary>
        /// <param name="message">message to log</param>
        void LogError(string message);

        /// <summary>
        ///     Logs a normal message to the context stream
        /// </summary>
        /// <param name="message">message to log</param>
        void Log(string message);

        /// <summary>
        ///     Logs a normal message to the context stream
        /// </summary>
        /// <param name="message">message to log</param>
        void LogVerbose(string message);

        /// <summary>
        ///     Gets the log entries
        /// </summary>
        /// <param name="filter">entry type filter</param>
        /// <returns>resulting value</returns>
        string GetLogs(EntryTypes filter);

        /// <summary>
        ///     Increments the value of the named counter
        /// </summary>
        /// <param name="name">counter name</param>
        /// <param name="instanceName">counter instance name</param>
        /// <param name="instanceNameSuffix">counter instance name suffix</param>
        /// <param name="value">value to incrememnt the existing value by</param>
        void IncrementCounter(
            string name,
            string instanceName,
            string instanceNameSuffix,
            ulong value);

        /// <summary>
        ///      Reports the action event
        /// </summary>
        /// <param name="eventType">event type</param>
        /// <param name="actionType">action type</param>
        /// <param name="actionName">action name</param>
        /// <param name="data">event data</param>
        void ReportActionEvent(
            string eventType,
            string actionType,
            string actionName,
            IDictionary<string, string> data);

        /// <summary>
        ///      Reports the action event
        /// </summary>
        /// <param name="eventType">event type</param>
        /// <param name="actionType">action type</param>
        /// <param name="actionName">action name</param>
        /// <param name="message">error message</param>
        /// <param name="data">event data</param>
        void ReportActionError(
            string eventType,
            string actionType,
            string actionName,
            string message,
            IDictionary<string, string> data);

        /// <summary>
        ///     Gets the value of an extension property
        /// </summary>
        /// <param name="groupName">property group name</param>
        /// <param name="name">property name</param>
        /// <returns>resulting value or null if the value does not exist</returns>
        string GetExtensionPropertyValue(
            string groupName,
            string name);
    }
}
