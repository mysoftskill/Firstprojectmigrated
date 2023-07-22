namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Telemetry;

    /// <summary>
    /// Writes successful request events.
    /// </summary>
    public class SuccessSessionWriter : BaseSessionWriter<OperationMetadata>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuccessSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public SuccessSessionWriter(ILogger<Base> log, SessionProperties properties)
            : base(log, properties)
        {
        }

        /// <summary>
        /// Creates a successful request SLL event and logs it.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The request metadata.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, OperationMetadata data)
        {
            var sllEvent = new IncomingApiSuccessEvent();

            // Operation metadata
            sllEvent.baseData.SetOperationMetadata(data);

            this.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, data.ProtocolStatusCode.ToString());
        }
    }
}
