namespace Microsoft.PrivacyServices.DataManagement.Worker
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Newtonsoft.Json;
    using Telemetry;

    /// <summary>
    /// Converts LockStatus{T} data into SLL events.
    /// </summary>
    /// <typeparam name="T">The lock state type.</typeparam>
    public class LockStatusSessionWriter<T> : BaseSessionWriter<Tuple<Lock<T>, string>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockStatusSessionWriter{T}" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public LockStatusSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// Converts the response into an SLL event and logs it.
        /// </summary>
        /// <param name="status">The parameter is not used.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the request.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The data to convert.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<Lock<T>, string> data)
        {
            var sllEvent = new WorkItemSuccessEvent();

            if (data.Item1 != null)
            {
                sllEvent.documentId = data.Item1.Id;
                sllEvent.etag = data.Item1.ETag;
                sllEvent.expiryTime = data.Item1.ExpiryTime.UtcDateTime.ToString("O");
                sllEvent.workerId = data.Item1.WorkerId.ToString();
                sllEvent.state = JsonConvert.SerializeObject(data.Item1.State, Formatting.None);
            }

            // Session is a Success if we got the lock and completed the action (data.Item2 == null) or
            // we are waiting for something (Lock, Stream, Changes, etc.)
            string statusCode = data.Item2;
            status = string.IsNullOrEmpty(statusCode) || statusCode.StartsWith("Wait") || statusCode.Equals("Completed") || statusCode.Equals("Disabled") ? SessionStatus.Success : SessionStatus.Error;

            this.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, statusCode);
        }

    }
}