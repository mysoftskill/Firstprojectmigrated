namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// A controller that is used to map HTTP errors into service errors.
    /// </summary>
    public class ErrorController : ApiController
    {
        /// <summary>
        /// Converts a 404 page not found into a handled service error.
        /// </summary>
        /// <returns>The error.</returns>
        [HttpGet, HttpPost, HttpPut, HttpDelete, HttpHead, HttpOptions, AcceptVerbs("PATCH")]
        public HttpResponseMessage Handle404()
        {
            throw new InvalidRequestError();
        }
    }
}