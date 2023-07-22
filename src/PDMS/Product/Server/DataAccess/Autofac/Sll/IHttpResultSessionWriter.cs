namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Telemetry;

    /// <summary>
    /// Converts document client exception data into SLL events.
    /// </summary>
    public class IHttpResultSessionWriter : BaseSessionWriter<IHttpResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IHttpResultSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public IHttpResultSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// Converts the response into an SLL event and logs it.
        /// All sessions with a resource response will be success events.
        /// </summary>
        /// <param name="status">The parameter is not used.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the request.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The data to convert.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, IHttpResult data)
        {
            var sllEvent = new IHttpResultEvent();
            sllEvent.baseData.requestMethod = data.RequestMethod.ToString();
            sllEvent.baseData.targetUri = data.RequestUrl;
            sllEvent.requestBody = data.RequestBody;
            sllEvent.responseBody = data.ResponseContent;

            this.LogOutGoingEvent(sllEvent, status, $"{name}.{data.OperationName}", data.DurationMilliseconds, cv, data.HttpStatusCode.ToString());
        }
    }
}