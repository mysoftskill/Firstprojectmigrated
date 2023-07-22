namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.Authentication;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Web.Http;
    using System;

    /// <summary>
    ///     A controller to serve up the OpenApi spec for PXS.
    /// </summary>
    [RoutePrefix("")]
    public class OpenApiDocumentController : ApiController
    {
        private static string documentText = null;

        public OpenApiDocumentController()
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
        ///     An API that always returns a copy of the OpenApi spec for this service.
        /// </summary>
        /// <remarks>May return <see cref="HttpStatusCode">NotFound</see> if the document cannot be found.</remarks>
        /// <returns>An action result representing a 200 OK success response.</returns>
        /// <group>Swagger</group>
        /// <verb>get</verb>
        /// <url>https://pcf.privacy.microsoft.com/v1/openapi</url>
        [HttpGet]
        [OverrideAuthentication]
        [ProductionAuthorize]
        [Route("v1/openapi")]
        [IncomingRequestActionFilter("API", "OpenApiDocument", "1.0")]
        public HttpResponseMessage GetOpenApiDocument()
        {
            try
            {
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(documentText, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(ex.Message, Encoding.UTF8, "text/html");
                return response;
            }
        }
    }
}
