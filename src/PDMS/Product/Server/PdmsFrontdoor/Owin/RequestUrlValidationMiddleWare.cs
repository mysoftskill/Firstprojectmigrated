namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// An OWIN middleware class that validates URLs. Requests will not be processed if
    /// they don't start with API or probe.
    /// </summary>
    public class RequestUrlValidationMiddleWare : OwinMiddleware
    {
        /// <summary>
        /// The next middleware in the stack.
        /// </summary>
        private readonly OwinMiddleware next;

        /// <summary>
        /// Supported URL regexes.
        /// </summary>
        private readonly IList<Regex> supportedUrlRegexes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestUrlValidationMiddleWare" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the stack.</param>
        /// <param name="owinConfig">The OWIN config.</param>
        public RequestUrlValidationMiddleWare(OwinMiddleware next, IOwinConfiguration owinConfig) : base(next)
        {
            this.next = next;
            this.supportedUrlRegexes = new List<Regex>();

            foreach (var url in owinConfig.ValidRequestUrls)
            {
                this.supportedUrlRegexes.Add(new Regex(url, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }

        /// <summary>
        /// Invokes the middleware behavior. Skip processing requests if 
        /// they don't start with API or probe.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>A task.</returns>
        public override Task Invoke(IOwinContext context)
        {
            string uri = context.Request.Uri.AbsolutePath;
            
            foreach (Regex rgx in this.supportedUrlRegexes)
            {
                if (rgx.IsMatch(uri))
                {
                    return this.next.Invoke(context);
                }
            }

            return Task.CompletedTask;
        }
    }
}
