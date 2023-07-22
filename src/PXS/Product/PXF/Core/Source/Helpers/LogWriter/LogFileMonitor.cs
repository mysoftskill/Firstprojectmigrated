// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.LogWriter
{
    using System;
    using System.Threading;

    /// <summary>
    /// Provides events and counters for services to monitor and react to the internals
    /// of the file logging system.
    /// </summary>
    public static partial class LogFileMonitor
    {
        // NOTE: This class is stubbed out and has no implementation. Should we choose to implement more perf counters and telemetry, we can implement it here.

        /// <summary>
        /// Gets the number of events discarded since startup. Events can be discarded due
        /// to either the processing queue being full or a timeout when the file closes.
        /// </summary>
        public static long EventsDiscarded
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of events sent to the logger since startup.
        /// </summary>
        public static long EventsEncountered
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of events blocked by the filter since startup.
        /// </summary>
        public static long EventsFiltered
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of events currently in the processing queue.
        /// </summary>
        public static long EventsInQueue
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of events rejected by the processing queue because it was full since startup.
        /// </summary>
        public static long EventsRejected
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of events written to disk since startup.
        /// </summary>
        public static long EventsWritten
        {
            get { return 0; }
        }

        internal static void IncrementEventsEncountered()
        {
        }
    
        internal static void IncrementEventsInQueue()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "amount")]
        internal static void IncrementEventsInQueue(long amount)
        {
        }

        internal static void IncrementEventsDiscarded()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "amount")]
        internal static void IncrementEventsDiscarded(long amount)
        {
        }

        internal static void IncrementEventsRejected()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "amount")]
        internal static void IncrementBytesWritten(long amount)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "amount")]
        internal static void IncrementEventsWritten(long amount)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "filePath"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "index"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "topic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender")]
        internal static void OnFileCreated(object sender, string topic, int index, string filePath)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "exception"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "filePath"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "itemsQueued"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "index"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "topic")]
        internal static void OnWriteError(object sender, string topic, int index, string filePath, long itemsQueued, Exception exception)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "filePath"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "index"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "topic")]
        internal static void OnFileExpired(object sender, string topic, int index, string filePath)
        {
        }
    }
}
