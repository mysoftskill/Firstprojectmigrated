namespace Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation
{
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Owin;

    /// <summary>
    /// The CorrelationContextMiddleware ensures that the correlation context property bag is read from the http headers 
    /// and set to the response headers.
    /// </summary>
    public class CorrelationContextMiddleware : OwinMiddleware
    {
        /// <inheritdoc />
        public CorrelationContextMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        /// <inheritdoc />
        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationContext.HeaderName, out string[] correlationContextValues) &&
                correlationContextValues.Any() &&
                !string.IsNullOrWhiteSpace(correlationContextValues.FirstOrDefault()))
            {
                string ccStr = correlationContextValues[0];
                IOwinResponse response = context.Response;

                // Add on the Cc to the response.
                context.Response.OnSendingHeaders(
                    state => response.Headers.Set(CorrelationContext.HeaderName, ccStr),
                    null);

                Sll.Context.CorrelationContext = new CorrelationContext(ccStr);
            }

            await this.Next.Invoke(context);
        }
    }
}
