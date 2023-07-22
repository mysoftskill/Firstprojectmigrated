namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System;

    using Microsoft.DataPlatform.DataDiscoveryService.Contracts;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    using Telemetry;

    /// <summary>
    /// Converts datagrid search data into SLL events.
    /// NOTE:  This is not currently being used.
    /// </summary>
    public class DataGridResultSessionWriter : BaseSessionWriter<Tuple<SearchResponse, string, string>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridResultSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public DataGridResultSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<SearchResponse, string, string> data)
        {
            var sllEvent = new DataGridResultEvent();
            sllEvent.totalHits = data.Item1.TotalHits;
            sllEvent.assetType = data.Item2;
            sllEvent.searchTerms = data.Item3;

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "success");
        }
    }
}
