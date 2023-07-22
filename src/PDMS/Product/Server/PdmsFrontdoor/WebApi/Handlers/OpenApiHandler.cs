namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A message handler implementing the Probe.
    /// </summary>
    public class OpenApiHandler : BaseDelegatingHandler
    {
        private static string documentText = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiHandler" /> class.
        /// </summary>
        public OpenApiHandler() : base()
        {
            try
            {
                string rawJson = System.IO.File.ReadAllText(@".\OpenApiDocument.OpenApi3_0.json");

                documentText = JToken.Parse(rawJson).ToString(Formatting.Indented);
            }
            catch (Exception)
            {
                documentText = "OpenApiDocument.OpenApi3_0.json not found.";
            }
        }

        /// <summary>
        /// Calls the probe function. If an exception is thrown, it will be caught by the ServiceExceptionHandler
        /// and converted into a service fault, which will return a 500 error.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response object.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);

            response.Content = new StringContent(documentText, Encoding.UTF8, "application/json");

            return Task.FromResult(response);
        }
    }
}