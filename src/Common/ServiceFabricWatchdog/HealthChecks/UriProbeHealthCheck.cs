using System;
using System.Fabric.Health;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WatchdogSvc
{
    /// <summary>
    /// Uri probe health check.
    /// </summary>
    public class UriProbeHealthCheck : HealthCheckBase
    {
        private const string propertyName = "UriProbeHealthCheck";
        private readonly Uri probeUri;

        /// <summary>
        /// Create uri probe health check.
        /// </summary>
        /// <param name="uri">Target probe uri path.</param>
        public UriProbeHealthCheck(Uri uri)
            : base(propertyName)
        {
            this.probeUri = uri;
        }

        /// <inheritdoc />
        protected override async Task CheckCoreAsync()
        {
            ServiceEventSource.Current.Trace("UriProbeHealthCheck.CheckCoresAsync()");
            using HttpClientHandler handler = new HttpClientHandler();

            handler.ServerCertificateCustomValidationCallback = delegate{ return true; }; //lgtm[cs/do-not-disable-cert-validation] Suppressing warning because this is an internal SF health check

            using HttpClient client = new HttpClient(handler);
            try
            {
                var response = await client.GetAsync(this.probeUri);
                this.HealthState = HealthState.Ok;
                this.HealthDescription = $"{this.probeUri}={response.StatusCode}";

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    this.HealthState = HealthState.Error;
                }
            }
            catch (HttpRequestException ex)
            {
                // catch and log http request exception
                this.HealthState = HealthState.Error;
                var message = $"{this.probeUri}=\"{ex.Message}\"";
                this.HealthDescription = message;
            }
        }
    }
}
