namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.Telemetry;
    using System;

    /// <summary>
    /// Logs an SLL event for <c>Exceptions</c> from Azure Cloud Queue.
    /// </summary>
    public class CloudQueueExceptionSessionWriter : BaseSessionWriter<CloudQueueException>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueExceptionSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public CloudQueueExceptionSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// The required function from the ISessionWriter interface.
        /// Derived classes must implement the appropriate mapping from the TData type into an SLL data type.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The original data.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, CloudQueueException data)
        {
            var sllEvent = new CloudQueueExceptionEvent
            {
                message = data.EventData.Message?.AsString,
                messageCount = data.EventData.MessageCount?.ToString(),
                primaryUri = data.EventData.PrimaryUri,
                secondaryUri = data.EventData.SecondaryUri,
                queueName = data.EventData.QueueName,
                stackTrace = data.StackTrace,
                innerException = CreateInnerException(data.InnerException)
            };

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.GetName());
        }

        /// <summary>
        /// Creates and sets the inner exception data of the SLL event.
        /// </summary>
        /// <remarks>
        /// This function is recursive, but that is ok because exceptions do not typically have a large number of inner exceptions.
        /// </remarks>
        /// <param name="data">The exception data to store.</param>
        /// <param name="depth">The recursive depth.</param>
        /// <returns>The mapped object.</returns>
        internal static InnerException CreateInnerException(Exception data, int depth = 0)
        {
            if (data != null && depth < 5)
            {
                var innerException = new InnerException();
                innerException.code = data.GetName();
                innerException.message = data.Message;
                innerException.innerException = CreateInnerException(data.InnerException, depth++);
                return innerException;
            }
            else
            {
                return null;
            }
        }
    }
}