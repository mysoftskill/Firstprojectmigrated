namespace Microsoft.Azure.ComplianceServices.Common.Owin
{
    using Microsoft.Owin;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Caching;

    /// <summary>
    /// Add "x-content-type-options: nosniff" header to all http responses.
    /// </summary>
    internal class NoSniffXContentTypeOptionsMiddleware : OwinMiddleware
    {
        public NoSniffXContentTypeOptionsMiddleware(OwinMiddleware next) : base(next)
        {
        }

        /// <summary>
        /// Add "x-content-type-options: nosniff" header to all http responses.
        /// </summary>
        public override async Task Invoke(IOwinContext context)
        {
            var response = context.Response;
            response.OnSendingHeaders(
                state =>
                {
                    response.Headers["X-Content-Type-Options"] = "nosniff";
                    // Set the charset attribute for the Content-Type header.
                    if (response.ContentType != null &&
                        response.ContentType.StartsWith("application/json") && !response.ContentType.ToLower().Contains("charset"))
                    {
                        response.ContentType += "; charset=utf-8";
                    }
                },
                null);

            await this.Next.Invoke(context);
        }
    }
}