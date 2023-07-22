namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs the successful result data for GetMemberGroups.
    /// </summary>
    public class GetByIdsCollectionPageSessionWriter : BaseSessionWriter<IDirectoryObjectGetByIdsCollectionPage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetByIdsCollectionPageSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public GetByIdsCollectionPageSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// Logs the result and duration of the session.
        /// </summary>
        /// <param name="status">How the state should be classified.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="totalMilliseconds">How long it took for the operation to complete.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The state data that may be logged for debug purposes.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, IDirectoryObjectGetByIdsCollectionPage data)
        {
            var sllEvent = new MicrosoftGraphSuccessEvent();
            sllEvent.nextPageRequestUrl = data.NextPageRequest?.RequestUrl;
            sllEvent.count = data.Count;

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
        }
    }
}