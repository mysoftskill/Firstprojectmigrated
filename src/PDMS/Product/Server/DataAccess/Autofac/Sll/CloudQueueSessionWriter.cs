namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.Telemetry;

    /// <summary>
    /// Converts IHttpResult{PagedDeleteRequests} data into SLL events.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CloudQueueSessionWriter : BaseSessionWriter<CloudQueueEvent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public CloudQueueSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// Converts the IHttpResult{PagedDeleteRequests} response into an SLL event and logs it.
        /// </summary>
        /// <param name="status">The parameter is not used.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the request.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The data to convert.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, CloudQueueEvent data)
        {
            var sllEvent = new CloudQueueSuccessEvent
            {
                message = data.Message?.AsString,
                messageCount = data.MessageCount?.ToString(),
                primaryUri = data.PrimaryUri,
                secondaryUri = data.SecondaryUri,
                queueName = data.QueueName
            };

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
        }
    }
}
