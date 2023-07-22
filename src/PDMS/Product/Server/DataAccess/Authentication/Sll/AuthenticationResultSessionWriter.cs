namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;


    /// <summary>
    /// Logs the successful result data for AcquireTokenAsync.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AuthenticationResultSessionWriter : BaseSessionWriter<AuthenticationResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResultSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public AuthenticationResultSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
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
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, AuthenticationResult data)
        {
            var sllEvent = new MsalSuccessEvent();
            sllEvent.accessTokenType = data.TokenType;
            sllEvent.expiresOn = data.ExpiresOn.ToString();
            sllEvent.isExtendedLifeTimeToken = data.IsExtendedLifeTimeToken;
            sllEvent.tenantId = data.TenantId;
            sllEvent.displayableId = data.Account?.HomeAccountId.ToString();
            sllEvent.identityProvider = data.Account?.Environment;
            sllEvent.uniqueId = data.UniqueId;
            sllEvent.scopes = String.Join(" ", data.Scopes);

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
        }
    }
}