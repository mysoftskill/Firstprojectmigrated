namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps a DelegatingHandler with a proxy so that proper dependency injection may be used.
    /// It initializes a new scope before invoking the dependency resolver so that the lifetime of the objects are tied to the request.
    /// </summary>
    /// <typeparam name="TDelegatingHandler">The proxy handler type.</typeparam>
    public class DelegatingHandlerProxy<TDelegatingHandler> : DelegatingHandler
        where TDelegatingHandler : DelegatingHandler
    {
        /// <summary>
        /// Invokes a new dependency scope and then calls the proxy handler logic.
        /// This ensures proper dependency injection for the proxy handler.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response data.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Trigger the creation of the scope.
            // This ties object lifetime management to the individual request.
            var scope = request.GetDependencyScope();
                        
            var handler = scope.GetService(typeof(TDelegatingHandler)) as TDelegatingHandler;

            if (this.InnerHandler != null && handler.InnerHandler == null)
            {
                handler.InnerHandler = this.InnerHandler;
            }

            var invoker = new HttpMessageInvoker(handler);

            var response = await invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);
            
            return response;
        }
    }
}
