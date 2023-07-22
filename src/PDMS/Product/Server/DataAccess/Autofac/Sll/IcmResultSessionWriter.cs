namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using System;

    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    using Telemetry;

    /// <summary>
    /// Converts document client exception data into SLL events.
    /// </summary>
    public class IcmResultSessionWriter : BaseSessionWriter<Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IcmResultSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public IcmResultSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string> data)
        {
            var sllEvent = new IcmResultEvent();
            sllEvent.connectorId = data.Item1.ToString();
            sllEvent.eventName = data.Item2.CorrelationId;

            if (data.Item3 != null)
            {
                sllEvent.incidentId = data.Item3.IncidentId?.ToString();

                this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "success");
            }
            else
            {
                this.LogOutGoingEvent(sllEvent, SessionStatus.Error, name, totalMilliseconds, cv, data.Item4);
            }
        }
    }
}