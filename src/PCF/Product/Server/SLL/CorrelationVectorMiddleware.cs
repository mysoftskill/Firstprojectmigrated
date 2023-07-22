namespace Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation
{
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Owin;

    /// <summary>
    /// The CorrelationVersionMiddleware ensures that the correlation vector is read from the http headers, 
    /// extended and set back to the response headers. Logs an event if a request with no correlation vector is received.
    /// </summary>
    public class CorrelationVectorMiddleware : OwinMiddleware
    {
        /// <inheritdoc />
        public CorrelationVectorMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        /// <inheritdoc />
        public override async Task Invoke(IOwinContext context)
        {
            CorrelationVector cv = null;

            if (context.Request.Headers.ContainsKey(CorrelationVector.HeaderName))
            {
                string vector = context.Request.Headers.GetValues(CorrelationVector.HeaderName).FirstOrDefault();
                if (!string.IsNullOrEmpty(vector))
                {
                    cv = CorrelationVector.Extend(vector);
                }
            }

            cv = cv ?? new CorrelationVector();
            Sll.Context.Vector = cv;
            IOwinResponse response = context.Response;

            // Add on the CV to the response.
            context.Response.OnSendingHeaders(
                state => response.Headers.Set(CorrelationVector.HeaderName, cv.Value),
                null);

            new RequestFirstReceivedEvent
            {
                TargetUri = context.Request.Uri.ToString(),
                CV = cv.Value,
                ClientIp = context.Request?.RemoteIpAddress
            }.LogCritical();

            await this.Next.Invoke(context);
        }
    }
}
