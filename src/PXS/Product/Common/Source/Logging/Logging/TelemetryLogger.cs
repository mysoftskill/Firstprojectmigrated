// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Logging.Logging
{
    using System;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Telemetry;

    /// <summary>
    ///     allows for logging of telemetry events
    /// </summary>
    public class TelemetryLogger : ITelemetryLogger
    {
        /// <summary>
        ///     Logs an error event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        public void LogError<T>(
            T @event, 
            Action<Envelope> envelopeFiller)
            where T : Base
        {
            @event.LogError(envelopeFiller);
        }

        /// <summary>
        ///     Logs an error event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        public void LogError<T>(T @event)
            where T : Base
        {
            @event.LogError(LoggingInfo.FillEnvelope);
        }

        /// <summary>
        ///     Logs a warning event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        public void LogWarning<T>(
            T @event, 
            Action<Envelope> envelopeFiller)
            where T : Base
        {
            @event.LogWarning(envelopeFiller);
        }

        /// <summary>
        ///     Logs a warning event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        public void LogWarning<T>(T @event)
            where T : Base
        {
            @event.LogWarning(LoggingInfo.FillEnvelope);
        }

        /// <summary>
        ///     Logs an informational event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        /// <param name="envelopeFiller">fills the event envelope</param>
        public void LogInfo<T>(
            T @event, 
            Action<Envelope> envelopeFiller)
            where T : Base
        {
            @event.LogInformational(envelopeFiller);
        }

        /// <summary>
        ///     Logs an informational event
        /// </summary>
        /// <typeparam name="T">type of event to log</typeparam>
        /// <param name="event">event to log</param>
        public void LogInfo<T>(T @event)
            where T : Base
        {
            @event.LogWarning(LoggingInfo.FillEnvelope);
        }
    }
}
