namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A message handler implementing the HSTS server processing model as defined in RFC 6797.
    /// This ensures that HTTP calls are redirected and informs clients of this behavior with the appropriate header.
    /// This is necessary for the probe API because the probe agents can only use HTTP calls.
    /// </summary>
    public class HstsHandler : BaseDelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HstsHandler" /> class.
        /// </summary>
        public HstsHandler() : base()
        {
        }

        /// <summary>
        /// Modifies the response so that it includes the STS header.
        /// If the request occurs over HTTP then a redirect response is returned.
        /// </summary>
        /// <param name="request">The request.</param>                               
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response with the header added.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri requestUri = request.RequestUri;
            HttpResponseMessage response;

            if (requestUri.Scheme == Uri.UriSchemeHttps)
            {
                // section 7.1
                response = await this.BaseSendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response != null && response.Headers != null)
                {
                    // 31536000 seconds = 1 year.
                    response.Headers.Add("Strict-Transport-Security", new[]{"max-age=31536000; includeSubdomains"});
                }
            }            
            else if (requestUri.AbsolutePath == "/probe")
            {
                // The probe api must be excluded because the probe agent only works with http.
                response = await this.BaseSendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // section 7.2
                response = request.CreateResponse(HttpStatusCode.MovedPermanently);
                if (response.Headers != null)
                {
                    var path = "https://" + requestUri.Host + requestUri.PathAndQuery;
                    response.Headers.Add("Location", path);
                }
            }

            return response;
        }
    }
}