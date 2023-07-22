// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Logging.Logging
{
    using System;

    using Microsoft.Telemetry;

    /// <summary>
    ///     contract for object that allow logging of telemetry events
    /// </summary>
    public interface ITelemetryLogger
    {
        /// <summary>
        ///     Logs an error event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        void LogError<T>(
            T @event,
            Action<Envelope> envelopeFiller)
            where T : Base;

        /// <summary>
        ///     Logs an error event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        void LogError<T>(T @event)
            where T : Base;

        /// <summary>
        ///     Logs a warning event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        void LogWarning<T>(
            T @event, 
            Action<Envelope> envelopeFiller)
            where T : Base;

        /// <summary>
        ///     Logs a warning event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        void LogWarning<T>(T @event)
            where T : Base;

        /// <summary>
        ///     Logs an informational event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        void LogInfo<T>(
            T @event, 
            Action<Envelope> envelopeFiller)
            where T : Base;

        /// <summary>
        ///     Logs an informational event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        void LogInfo<T>(T @event)
            where T : Base;
    }
}
