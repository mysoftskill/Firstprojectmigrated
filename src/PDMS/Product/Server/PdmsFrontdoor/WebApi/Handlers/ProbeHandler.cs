namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A message handler implementing the Probe.
    /// </summary>
    public class ProbeHandler : BaseDelegatingHandler
    {
        private readonly IProbeMonitor probe;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbeHandler" /> class.
        /// </summary>
        /// <param name="probe">A function to call that invokes the probe.</param>
        public ProbeHandler(IProbeMonitor probe)
        {
            this.probe = probe;
        }

        /// <summary>
        /// Calls the probe function. If an exception is thrown, it will be caught by the ServiceExceptionHandler
        /// and converted into a service fault, which will return a 500 error.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response object.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await this.probe.ProbeAsync(cancellationToken).ConfigureAwait(false);

            var response = request.CreateResponse(HttpStatusCode.OK);

            // Set the size of response body and content type as required by Akamai change.
            var content = new string(new char[8 * 1024]);
            response.Content = new StringContent(content, Encoding.UTF8, "text/html");

            return response;
        }
    }
}