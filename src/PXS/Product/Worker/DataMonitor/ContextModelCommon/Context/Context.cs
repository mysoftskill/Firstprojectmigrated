// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.DataActionRunner.Telemetry;

    /// <summary>
    ///     operation context
    /// </summary>
    public sealed class Context :
        IExecuteContext,
        IParseContext
    {
        private readonly Stack<(ActionType Type, string Tag)> tagStack = new Stack<(ActionType, string)>();
        private readonly Stack<Func<string>> errPrefixesStack = new Stack<Func<string>>();
        private readonly List<LogEntry> entries = new List<LogEntry>();

        private readonly IDictionary<string, IDictionary<string, string>> extProps;

        private readonly ITelemetryLogger telemetryLogger;
        private readonly ICounterFactory counterFactory;

        private readonly string contextHostName;

        private readonly IClock clock;

        /// <summary>
        ///     Initializes a new instance of the Context class
        /// </summary>
        /// <param name="clock">time of day clock</param>
        public Context(IClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.OperationStartTime = this.clock.UtcNow;
        }

        /// <summary>
        ///     Initializes a new instance of the Context class
        /// </summary>
        /// <param name="extensionProperties">extension properties</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="isSimulation">true to is simulation; false otherwise</param>
        /// <param name="clock">time of day clock</param>
        /// <param name="telemetryLogger">telemetry logger</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="contextHostName">context host name</param>
        public Context(
            IDictionary<string, IDictionary<string, string>> extensionProperties,
            CancellationToken cancellationToken,
            bool isSimulation,
            IClock clock,
            ITelemetryLogger telemetryLogger,
            ICounterFactory counterFactory,
            string contextHostName)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.CancellationToken = cancellationToken;
            this.IsSimulation = isSimulation;
            this.extProps = extensionProperties;

            this.OperationStartTime = this.clock.UtcNow;

            this.telemetryLogger = telemetryLogger;
            this.counterFactory = counterFactory;

            this.contextHostName = contextHostName;
        }

        /// <summary>
        ///     Gets the cancellation token
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        ///     Gets the time that the action started processing at
        /// </summary>
        public DateTimeOffset OperationStartTime { get; }

        /// <summary>
        ///     Gets the current time (UTC)
        /// </summary>
        public DateTimeOffset NowUtc => this.clock.UtcNow;

        /// <summary>
        ///     Gets the current duration of the operation
        /// </summary>
        public TimeSpan Duration => this.clock.UtcNow - this.OperationStartTime;

        /// <summary>
        ///     Gets a value indicating whether this instance is a simulation
        /// </summary>
        /// <remarks>
        ///     Simulations do not perform actions that can trigger calls, send email, etc, but do perform read-only
        ///      actions (such as read only database queries)     
        /// </remarks>
        public bool IsSimulation { get; }

        /// <summary>
        ///     Gets a tag indicating where in processing 
        /// </summary>
        public string Tag => this.tagStack.Count > 0 ? this.tagStack.Peek().Tag : "<<CONTEXT EMPTY>>";

        /// <summary>
        ///     Gets a value indicating whether this errors have been logged to this context
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        ///     Pushes a new tag onto the stack
        /// </summary>
        /// <param name="type">action type</param>
        /// <param name="tag">tag of action being started</param>
        public void OnActionStart(
            ActionType type,
            string tag)
        {
            string fullTag = this.tagStack.Count > 0 ?
                this.tagStack.Peek().Tag + "." + tag :
                tag;

            this.tagStack.Push((type, fullTag));
        }

        /// <summary>
        ///     Updates the most recent tag
        /// </summary>
        /// <param name="tag">updated tag</param>
        public void OnActionUpdate(string tag)
        {
            if (this.tagStack.Count > 0)
            {
                // first, get rid of the most current tag, but grab the type out of it (as we expect this to be called to update
                //  only the tag and not the type)
                this.OnActionStart(this.tagStack.Pop().Type, tag);
            }
        }

        /// <summary>
        ///     Gets the value of an extension property
        /// </summary>
        /// <param name="groupName">property group name</param>
        /// <param name="name">property name</param>
        /// <returns>resulting value or null if the value does not exist</returns>
        public string GetExtensionPropertyValue(
            string groupName, 
            string name)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(groupName, nameof(groupName));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(name, nameof(name));

            if (this.extProps != null &&
                this.extProps.TryGetValue(groupName, out IDictionary<string, string> group) &&
                group != null &&
                group.TryGetValue(name, out string result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        ///     Discards the most recent tag 
        /// </summary>
        public void OnActionEnd()
        {
            if (this.tagStack.Count > 0)
            {
                this.tagStack.Pop();
            }
        }

        /// <summary>
        ///     Adds a message to the list of messages to be added before any reported parse error
        /// </summary>
        /// <param name="messageGenerator">message to add</param>
        /// <remarks>
        ///     this allows a parent action to set a conditional context setting message in the event one of its child actions
        ///      reports a parse error.  If no error is reported, this message is not emitted
        /// </remarks>
        public void PushErrorIntroMessage(Func<string> messageGenerator)
        {
            this.errPrefixesStack.Push(messageGenerator);
        }

        /// <summary>
        ///     Clears the last error intro message
        /// </summary>
        public void PopErrorIntroMessage()
        {
            if (this.errPrefixesStack.Count > 0)
            {
                this.errPrefixesStack.Pop();
            }
        }

        /// <summary>
        ///     Logs an exception
        /// </summary>
        /// <param name="exception">exception to log</param>
        /// <param name="notes">notes about the exception</param>
        public void LogError(
            Exception exception,
            string notes)
        {
            this.HasErrors = true;
            this.AppendAndClearErrorPrefixes();
            this.AddEntry(EntryTypes.Error, notes + ": " + exception.Message);
        }

        /// <summary>
        ///     Logs an error
        /// </summary>
        /// <param name="message">message to log with the exception</param>
        public void LogError(string message)
        {
            this.HasErrors = true;
            this.AppendAndClearErrorPrefixes();
            this.AddEntry(EntryTypes.Error, message);
        }

        /// <summary>
        ///     Logs a message to the context stream
        /// </summary>
        /// <param name="message">message to log</param>
        public void Log(string message)
        {
            this.AddEntry(EntryTypes.Normal, message);
        }

        /// <summary>
        ///     Logs a normal message to the context stream
        /// </summary>
        /// <param name="message">message to log</param>
        public void LogVerbose(string message)
        {
            this.AddEntry(EntryTypes.Verbose, message);
        }

        /// <summary>
        ///     Gets the log entries
        /// </summary>
        /// <param name="filter">entry type filter</param>
        /// <returns>resulting value</returns>
        public string GetLogs(EntryTypes filter)
        {
            StringBuilder sb = new StringBuilder();

            foreach (LogEntry e in this.entries.Where(o => (o.EntryType & filter) != 0))
            {
                sb.AppendLine(e.ToString(LogOutputFormat.Text));
            }

            return sb.ToString();
        }

        /// <summary>
        ///      Increments the value of the named counter
        /// </summary>
        /// <param name="name">counter name</param>
        /// <param name="instanceName">counter instance name</param>
        /// <param name="instanceNameSuffix">counter instance name suffix</param>
        /// <param name="value">value to incrememnt the existing value by</param>
        public void IncrementCounter(
            string name,
            string instanceName,
            string instanceNameSuffix,
            ulong value)
        {
            ICounter c = this.counterFactory?.GetCounter(this.contextHostName, name, CounterType.Number);

            c?.IncrementBy(value);

            if (string.IsNullOrWhiteSpace(instanceName) == false)
            {
                if (string.IsNullOrWhiteSpace(instanceNameSuffix) == false)
                {
                    instanceName = instanceName + "/" + instanceNameSuffix;
                }

                c?.IncrementBy(value, instanceName);
            }
        }

        /// <summary>
        ///      Reports the action event
        /// </summary>
        /// <param name="eventType">event type</param>
        /// <param name="actionType">action type</param>
        /// <param name="actionName">action name</param>
        /// <param name="data">event data</param>
        public void ReportActionEvent(
            string eventType,
            string actionType,
            string actionName,
            IDictionary<string, string> data)
        {
            if (this.telemetryLogger != null)
            {
                this.telemetryLogger.LogInfo(
                    new ActionEvent
                    {
                        Operation = "event",
                        Details = "success",
                        TaskId = this.contextHostName,
                        Item = this.Tag,

                        EventType = eventType,

                        ActionName = actionName,
                        ActionType = actionType,
                        Data = data != null ? 
                            new Dictionary<string, string>(data) : 
                            new Dictionary<string, string>(),
                    });
            }
        }

        /// <summary>
        ///      Reports the action event
        /// </summary>
        /// <param name="eventType">event type</param>
        /// <param name="actionType">action type</param>
        /// <param name="actionName">action name</param>
        /// <param name="message">error message</param>
        /// <param name="data">event data</param>
        public void ReportActionError(
            string eventType,
            string actionType,
            string actionName,
            string message,
            IDictionary<string, string> data)
        {
            if (this.telemetryLogger != null)
            {
                this.telemetryLogger.LogError(
                    new ActionError
                    {
                        Operation = "event",
                        Details = message,
                        TaskId = this.contextHostName,
                        Item = this.Tag,

                        EventType = eventType,

                        ActionName = actionName,
                        ActionType = actionType,
                        Data = data != null ?
                            new Dictionary<string, string>(data) :
                            new Dictionary<string, string>(),
                    });
            }
        }
        
        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Adds the entry
        /// </summary>
        /// <param name="entryTypes">entry type</param>
        /// <param name="message">message</param>
        private void AddEntry(
            EntryTypes entryTypes,
            string message)
        {
            (ActionType Type, string Tag) ctx = this.tagStack.Peek();
            this.entries.Add(new LogEntry(this.clock.UtcNow, ctx.Type, entryTypes, ctx.Tag, message));
        }

        /// <summary>
        ///     Appends the error prefixes and clears the prefix stack
        /// </summary>
        private void AppendAndClearErrorPrefixes()
        {
            foreach (Func<string> gen in this.errPrefixesStack.Where(o => o != null).Reverse())
            {
                this.AddEntry(EntryTypes.Error, gen());
            }

            this.errPrefixesStack.Clear();
        }
    }
}
