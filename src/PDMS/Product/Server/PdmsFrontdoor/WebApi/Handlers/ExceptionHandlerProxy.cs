namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;

    /// <summary>
    /// Wraps an ExceptionHandler with a proxy so that proper dependency injection may be used.
    /// It initializes a new scope before invoking the dependency resolver so that the lifetime of the objects are tied to the request.
    /// </summary>
    /// <typeparam name="TExceptionHandler">The proxy handler type.</typeparam>
    public class ExceptionHandlerProxy<TExceptionHandler> : ExceptionHandler
        where TExceptionHandler : ExceptionHandler
    {
        /// <summary>
        /// Invokes a new dependency scope and then calls the proxy handler logic.
        /// This ensures proper dependency injection for the proxy handler.
        /// </summary>
        /// <param name="context">The context data.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        public override async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            // Trigger the creation of the scope.
            // This ties object lifetime management to the individual request.
            var scope = context.Request?.GetDependencyScope();

            var handler = scope?.GetService(typeof(TExceptionHandler)) as TExceptionHandler;

            if (handler != null)
            {
                await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
