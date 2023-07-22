namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Additional contextual information for the request.
    /// Required information is passed via the constructor.
    /// Optional values are available as properties with setters.
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestContext" /> class.
        /// </summary>
        public RequestContext()
        {
            this.CancellationToken = CancellationToken.None;
        }

        /// <summary>
        /// Gets or sets the correlation vector for the request.
        /// </summary>
        public string CorrelationVector { get; set; }

        /// <summary>
        /// Gets or sets the correlation context.
        /// </summary>
        public string CorrelationContext { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token for the request.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the authentication provider which will be used to generate an authentication token for this request.
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider { get; set; }

        /// <summary>
        /// Gets or sets the original client cert for the request if it is coming from Akamai.
        /// </summary>
        public string OriginalClientCert { get; set; }

        /// <summary>
        /// Gets or sets the original client IP address for the request if it is coming from Akamai.
        /// </summary>
        public string OriginalClientIPAddress { get; set; }

        /// <summary>
        /// Converts the request context into a set of headers.
        /// </summary>
        /// <returns>The header dictionary.</returns>
        public IDictionary<string, Func<Task<string>>> GetHeaders()
        {
            var headers = new Dictionary<string, Func<Task<string>>>();

            if (!string.IsNullOrWhiteSpace(this.CorrelationVector))
            {
                headers.Add("MS-CV", () => Task.FromResult(this.CorrelationVector));
            }

            if (!string.IsNullOrWhiteSpace(this.CorrelationContext))
            {
                headers.Add("Correlation-Context", () => Task.FromResult(this.CorrelationContext));
            }

            if (this.AuthenticationProvider != null)
            {
                headers.Add(
                    "Authorization",
                    async () =>
                    {
                        var header = await this.AuthenticationProvider.AcquireTokenAsync(this.CancellationToken).ConfigureAwait(false);
                        return header.ToString();
                    });
            }

            if (this.OriginalClientCert != null)
            {
                headers.Add("X-CERTPEM", () => Task.FromResult(this.OriginalClientCert));
            }

            if (this.OriginalClientIPAddress != null)
            {
                headers.Add("True-Client-IP", () => Task.FromResult(this.OriginalClientIPAddress));
            }

            return headers;
        }
    }
}