namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll
{
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto;
    using Microsoft.Telemetry;

    public class KustoResponseSessionWriter : BaseSessionWriter<IHttpResult<KustoResponse>>
    {
        /// <inheritdoc />
        public KustoResponseSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <inheritdoc />
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, IHttpResult<KustoResponse> data)
        {
            const string NullValue = "null";

            var sllEvent = new KustoResponseSuccessEvent();
            sllEvent.hasErrors = data.Response?.HasErrors?.ToString() ?? NullValue;
            sllEvent.version = data.Response?.Version ?? NullValue;
            sllEvent.tableName = data.Response?.TableName ?? NullValue;

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.HttpStatusCode.ToString());
        }
    }
}