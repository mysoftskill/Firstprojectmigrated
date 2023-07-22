namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Telemetry;
    using Error = Common.Configuration.Error;

    /// <summary>
    /// Converts document client exception data into SLL events.
    /// </summary>
    public class DocumentClientExceptionSessionWriter : BaseSessionWriter<Tuple<string, DocumentClientException>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientExceptionSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public DocumentClientExceptionSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<string, DocumentClientException> data)
        {
            var sllEvent = new DocumentClientErrorEvent();
            sllEvent.activityId = data.Item2.ActivityId;
            sllEvent.requestCharge = data.Item2.RequestCharge;
            sllEvent.message = data.Item2.Message;
            sllEvent.error = new DocumentClientError { code = data.Item2.Error.Code, message = data.Item2.Error.Message };
            sllEvent.retryAfter = data.Item2.RetryAfter.ToString();
            sllEvent.responseHeaders = new List<ResponseHeader>();
            sllEvent.baseData.targetUri = data.Item1;

            foreach (var key in data.Item2.ResponseHeaders.AllKeys)
            {
                sllEvent.responseHeaders.Add(new ResponseHeader { name = key, values = data.Item2.ResponseHeaders.GetValues(key).ToList() });
            }

            var statusCode = (int)data.Item2.StatusCode;
            if (statusCode < 500)
            {
                status = SessionStatus.Error;
            }
            else
            {
                status = SessionStatus.Fault;
            }

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.Item2.StatusCode.ToString());
        }
    }
}