namespace Microsoft.Azure.ComplianceServices.Common.Owin
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin;
 
    /// <summary>
    /// Disallow http request if HttpMethod is Head.
    /// </summary>
    internal class DisallowHttpMethodHeadMiddleware : OwinMiddleware
    {
        public DisallowHttpMethodHeadMiddleware(OwinMiddleware next) : base(next)
        {
        }

        /// <summary>
        /// Disallow http request if HttpMethod is Head.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>A task.</returns>
        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method == HttpMethod.Head.ToString())
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}