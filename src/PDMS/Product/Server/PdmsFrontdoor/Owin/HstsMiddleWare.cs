namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// An OWIN middleware class that returns the HSTS header.
    /// Use this in place of the MessageHandler version if 
    /// the behavior needs to happen earlier in the stack (due to other middleware).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Cannot mock the OnSendingHeaders callback.
    public class HstsMiddleWare : OwinMiddleware
    {
        /// <summary>
        /// The next middleware in the stack.
        /// </summary>
        private readonly OwinMiddleware next;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HstsMiddleWare" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the stack.</param>
        /// <param name="owinConfig">The OWIN config.</param>
        public HstsMiddleWare(OwinMiddleware next, IOwinConfiguration owinConfig) : base(next)
        {
            this.next = next;            
        }

        /// <summary>
        /// Invokes the middleware behavior. Skip processing requests if 
        /// they don't start with API or probe.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>A task.</returns>
        public override Task Invoke(IOwinContext context)
        {
            Uri requestUri = context.Request.Uri;

            if (requestUri.Scheme == Uri.UriSchemeHttps)
            {
                // section 7.1
                context.Response.OnSendingHeaders(
                    state =>
                    {
                        var response = (OwinResponse)state;

                        if (!response.Headers.ContainsKey("Strict-Transport-Security"))
                        {
                            response.Headers.Add("Strict-Transport-Security", new[]{"max-age=31536000; includeSubdomains"});
                        }
                    }, 
                    context.Response);

                return this.next.Invoke(context);
            }
            else if (requestUri.AbsolutePath == "/probe")
            {
                // The probe api must be excluded because the probe agent only works with http.
                return this.next.Invoke(context);
            }
            else
            {
                var path = "https://" + requestUri.Host + requestUri.PathAndQuery;

                // section 7.2
                context.Response.OnSendingHeaders(
                    state =>
                    {
                        var response = (OwinResponse)state;
                        response.StatusCode = 301; // Use 301 instead of 302.
                    }, 
                    context.Response);

                context.Response.Redirect(path);

                return Task.CompletedTask;
            }
        }
    }
}
