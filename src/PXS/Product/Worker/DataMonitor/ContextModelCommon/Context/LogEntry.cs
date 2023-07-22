// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     type of action being started
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        ///     invalid default option
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     the action being started is for parsing
        /// </summary>
        Parse,

        /// <summary>
        ///     the action being started is for expansion
        /// </summary>
        Expand,

        /// <summary>
        ///     the action being started is for validation
        /// </summary>
        Validate,

        /// <summary>
        ///     the action being started is for execution
        /// </summary>
        Execute
    }

    /// <summary>
    ///     type of the log entry
    /// </summary>
    /// <remarks>
    ///     this is implemented as flags for query purposes; in practice, only one flag should be set on a given entry
    /// </remarks>
    [Flags]
    public enum EntryTypes
    {
        /// <summary>
        ///     none option- this should never be used
        /// </summary>
        None = 0,

        /// <summary>
        ///     entry is an error
        /// </summary>
        Error = 0x1,

        /// <summary>
        ///     logs the title of processed items
        /// </summary>
        Title = 0x2,

        /// <summary>
        ///     entry is normal activity
        /// </summary>
        Normal = 0x4,

        /// <summary>
        ///     entry is a verbose logging note 
        /// </summary>
        Verbose = 0x8,

        /// <summary>
        ///     all entry types
        /// </summary>
        All = EntryTypes.Error | EntryTypes.Title | EntryTypes.Normal | EntryTypes.Verbose,
    }

    /// <summary>
    ///     LogOutputFormat enum
    /// </summary>
    public enum LogOutputFormat
    {
        /// <summary>
        ///     output in a standard text format
        /// </summary>
        Text = 0,

        /// <summary>
        ///     output as a comma separated list
        /// </summary>
        Csv,

        /// <summary>
        ///     output as a tab seprated list
        /// </summary>
        Tsv,
    }
    
    /// <summary>
    ///     log entry class
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        ///     Initializes a new instance of the LogEntry class
        /// </summary>
        /// <param name="time">log entry occurrance time</param>
        /// <param name="actionType">action type</param>
        /// <param name="entryType">entry type</param>
        /// <param name="contextTag">context tag</param>
        /// <param name="message">message to record</param>
        public LogEntry(
            DateTimeOffset time, 
            ActionType actionType,
            EntryTypes entryType, 
            string contextTag, 
            string message)
        {
            this.ContextTag = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(contextTag, nameof(contextTag));
            this.Message = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(message, nameof(message));

            this.ActionType = actionType;
            this.EntryType = entryType;
            this.Time = time;
        }

        /// <summary>
        ///     Gets the time that the entry occurred at
        /// </summary>
        public DateTimeOffset Time { get; }

        /// <summary>
        ///     Gets the type of the action generating the log entry
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        ///     Gets the log entry type
        /// </summary>
        public EntryTypes EntryType { get; }

        /// <summary>
        ///     Gets the time that the entry occurred at
        /// </summary>
        public string ContextTag { get; }

        /// <summary>
        ///     Gets the time that the entry occurred at
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <param name="format">format to emit as</param>
        /// <returns>string that represents this instance</returns>
        public string ToString(LogOutputFormat format)
        {
            string formatString;

            switch (format)
            {
                case LogOutputFormat.Text:
                    // this format string intentionally skips {2} and {3}
                    formatString = "{0:yyyy-MM-dd HH:mm:ss.fff}: {1}: {4}";
                    break;

                case LogOutputFormat.Csv:
                    formatString = "{0:yyyy-MM-dd HH:mm:ss.fff}, {1}, {2}, {3}, {4}";
                    break;

                case LogOutputFormat.Tsv:
                    formatString = "{0:yyyy-MM-dd HH:mm:ss.fff}\t{1}\t{2}\t{3}\t{4}";
                    break;

                default:
                    throw new ArgumentException(format + " is not a supported format mode");
            }

            return formatString.FormatInvariant(this.Time, this.ContextTag, this.EntryType, this.ActionType, this.Message);
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            return this.ToString(LogOutputFormat.Text);
        }
    }
}
