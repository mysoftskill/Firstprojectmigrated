namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using System;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Telemetry;

    /// <summary>
    /// Converts document client instrumentation data into SLL events.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    public class ResourceResponseSessionWriter<T> : BaseSessionWriter<Tuple<string, ResourceResponse<T>>>
        where T : Resource, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceResponseSessionWriter{T}" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public ResourceResponseSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<string, ResourceResponse<T>> data)
        {
            var sllEvent = new DocumentClientSuccessEvent();
            sllEvent.activityId = data.Item2.ActivityId;
            sllEvent.requestCharge = data.Item2.RequestCharge;
            sllEvent.baseData.targetUri = data.Item1;
            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
        }
    }
}