namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    public class DisallowHttpMethodHeadMiddleware : OwinMiddleware
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
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}
