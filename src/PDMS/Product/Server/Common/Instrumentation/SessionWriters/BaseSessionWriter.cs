namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Provides a base set of helper functions for all SLL session events.
    /// </summary>
    /// <typeparam name="TData">The business data type.</typeparam>
    public abstract class BaseSessionWriter<TData> : ISessionWriter<TData>
    {
        private const string Unknown = "Unknown";

        /// <summary>
        /// The log object.
        /// </summary>
        private readonly ILogger<Base> log;

        /// <summary>
        /// The session properties.
        /// </summary>
        private readonly SessionProperties properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSessionWriter{TData}" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public BaseSessionWriter(ILogger<Base> log, SessionProperties properties)
        {
            this.log = log;
            this.properties = properties;
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
        public abstract void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, TData data);

        /// <summary>
        /// Sets common properties for incoming events and logs the event to the telemetry stream.
        /// </summary>
        /// <typeparam name="T">The SLL event type.</typeparam>
        /// <param name="sllEvent">The SLL event data.</param>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="statusCode">The status code for the event. Shows up in XPERT as the error name.</param>
        protected void LogIncomingEvent<T>(T sllEvent, SessionStatus status, string name, long totalMilliseconds, string cv, string statusCode)
            where T : BaseIncomingSucessEvent
        {
            sllEvent.baseData.operationName = string.IsNullOrWhiteSpace(name) ? Unknown : name;
            sllEvent.baseData.latencyMs = (int)totalMilliseconds;
            sllEvent.baseData.SetSessionStatus(status);
            sllEvent.baseData.protocolStatusCode = statusCode;
            sllEvent.component = "Incoming";
            sllEvent.baseData.callerName = this.properties.PartnerName;
            this.log.Write(this.properties, sllEvent, status.ToEventLevel(), EventOptions.None, cv);
        }

        /// <summary>
        /// Sets common properties for incoming events and logs the event to the telemetry stream.
        /// </summary>
        /// <typeparam name="T">The SLL event type.</typeparam>
        /// <param name="sllEvent">The SLL event data.</param>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="statusCode">The status code for the event. Shows up in XPERT as the error name.</param>
        protected void LogInternalEvent<T>(T sllEvent, SessionStatus status, string name, long totalMilliseconds, string cv, string statusCode)
            where T : BaseIncomingSucessEvent
        {
            name = string.IsNullOrWhiteSpace(name) ? Unknown : name;
            sllEvent.baseData.operationName = $"_Internal.{name}";
            sllEvent.baseData.latencyMs = (int)totalMilliseconds;
            sllEvent.baseData.SetSessionStatus(status);
            sllEvent.baseData.protocolStatusCode = statusCode;
            sllEvent.component = "Internal";
            this.log.Write(this.properties, sllEvent, status.ToEventLevel(), EventOptions.None, cv);
        }

        /// <summary>
        /// Sets common properties for outgoing events and logs the event to the telemetry stream.
        /// Splits session name into partner name and API name by assuming the format: "Partner.API".
        /// </summary>
        /// <typeparam name="T">The SLL event type.</typeparam>
        /// <param name="sllEvent">The SLL event data.</param>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="statusCode">The status code for the event. Shows up in XPERT as the error name.</param>
        protected void LogOutGoingEvent<T>(T sllEvent, SessionStatus status, string name, long totalMilliseconds, string cv, string statusCode)
            where T : Microsoft.Telemetry.Data<Ms.Qos.OutgoingServiceRequest>
        {
            var nameParts = this.GetOutGoingApiParts(name);
            sllEvent.baseData.operationName = name;
            sllEvent.baseData.dependencyName = nameParts.Item1;
            sllEvent.baseData.dependencyOperationName = nameParts.Item2;
            sllEvent.baseData.latencyMs = (int)totalMilliseconds;
            sllEvent.baseData.SetSessionStatus(status);
            sllEvent.baseData.protocolStatusCode = statusCode;
            this.log.Write(this.properties, sllEvent, status.ToEventLevel(), EventOptions.None, cv);
        }

        /// <summary>
        /// Given a session name, split it into partner name and API name.
        /// Takes all values before the first period as the partner name.
        /// If there is no period, then partner name is set to Unknown.
        /// </summary>
        /// <param name="name">The session name.</param>
        /// <returns>Partner and API names as a tuple.</returns>
        protected Tuple<string, string> GetOutGoingApiParts(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Tuple.Create(Unknown, Unknown);
            }
            else
            { 
                var index = name.IndexOf('.');
                if (index > 0)
                {
                    var partner = name.Substring(0, index);
                    var api = name.Substring(index + 1);

                    if (string.IsNullOrWhiteSpace(api))
                    {
                        return Tuple.Create(Unknown, name);
                    }
                    else
                    {
                        return Tuple.Create(partner, api);
                    }
                }
                else
                {
                    return Tuple.Create(Unknown, name);
                }
            }
        }
    }
}
