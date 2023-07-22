// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks
{
    using System;
    using System.Runtime.Serialization;

    using Microsoft.PrivacyServices.Common.Telemetry;

    /// <summary>
    ///     base class for exceptions that can get raised by MultiInstanceTask tasks that include a telemetry
    ///      event to emit
    /// </summary>
    public class TaskTelemetryException : Exception
    {
        private const string EventName = "TELEMETRYEVENT";

        /// <summary>
        ///     Initializes a new instance of the TaskTelementryException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="event">telemetry event for the exception</param>
        public TaskTelemetryException(
            string message,
            TaskTelemetryEvent @event) :
            base(message)
        {
            this.Event = @event;
        }

        /// <summary>
        ///     Initializes a new instance of the TaskTelementryException class
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="exception">inner exception</param>
        /// <param name="event">telemetry event for the exception</param>
        public TaskTelemetryException(
            string message,
            Exception exception,
            TaskTelemetryEvent @event) :
            base(message, exception)
        {
            this.Event = @event;
        }

        /// <summary>
        ///     Initializes a new instance of the TaskTelementryException class
        /// </summary>
        /// <param name="event">telemetry event for the exception</param>
        public TaskTelemetryException(TaskTelemetryEvent @event)
        {
            this.Event = @event;
        }

        /// <summary>
        ///     Initializes a new instance of the TaskTelementryException class
        /// </summary>
        /// <param name="serializationInfo">serialization info</param>
        /// <param name="streamingContext">streaming context</param>
        protected TaskTelemetryException(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
            this.Event = serializationInfo
                .GetValue(TaskTelemetryException.EventName, typeof(TaskTelemetryEvent)) as TaskTelemetryEvent;
        }

        /// <summary>
        ///     Gets or sets the telemetry event associated with the exception
        /// </summary>
        public TaskTelemetryEvent Event { get; private set; }

        /// <summary>
        ///     Sets the SerializationInfo with information about the exception
        /// </summary>
        /// <param name="info">SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">StreamingContext that contains contextual information about the source or destination</param>
        public override void GetObjectData(
            SerializationInfo info, 
            StreamingContext context)
        {
            if (this.Event != null)
            {
                info.AddValue(TaskTelemetryException.EventName, this.Event, this.Event.GetType());
            }
        }
    }
}
