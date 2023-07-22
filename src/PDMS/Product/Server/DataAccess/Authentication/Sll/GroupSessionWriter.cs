namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs an SLL event for ServiceExceptions from the Microsoft graph.
    /// </summary>
    public class GroupSessionWriter : BaseSessionWriter<Group>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public GroupSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Group data)
        {
            var sllEvent = new MicrosoftGraphGroupFoundEvent
            {
                DisplayName = data?.DisplayName,
                SecurityEnabled = data?.SecurityEnabled?.ToString()
            };

            if (data != null)
            {
                if (data.SecurityEnabled == true)
                {
                    this.LogOutGoingEvent(sllEvent, SessionStatus.Success, name, totalMilliseconds, cv, "Success");
                }
                else
                {
                    this.LogOutGoingEvent(sllEvent, SessionStatus.Error, name, totalMilliseconds, cv, "GroupNotSecurityEnabled");
                }
            }
            else
            {
                this.LogOutGoingEvent(sllEvent, SessionStatus.Error, name, totalMilliseconds, cv, "GroupNotFound");
            }
        }
    }
}