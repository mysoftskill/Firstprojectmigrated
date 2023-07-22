// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common.Owin
{
    using Microsoft.Owin;
    using System.Threading.Tasks;

    public class StrictTransportSecurityMiddleware: OwinMiddleware
    {
        public StrictTransportSecurityMiddleware(OwinMiddleware next) : base(next)
        {
        }

        /// <summary>
        /// Add "Strict-Transport-Security: max-age=31536000; includeSubdomains" header to all http responses.
        /// </summary>
        public override async Task Invoke(IOwinContext context)
        {
            var response = context.Response;
            response.OnSendingHeaders(
                state =>
                {
                    response.Headers.Add("Strict-Transport-Security", new[] { "max-age=31536000; includeSubdomains" });
                },
                null);

            await this.Next.Invoke(context);
        }
    }
}
