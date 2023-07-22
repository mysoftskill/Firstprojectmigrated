namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;

    /// <summary>
    /// A proxy filter that injects dependencies into regular filters.
    /// </summary>
    /// <typeparam name="TActionFilter">The action filter type to resolve.</typeparam>
    public class ActionFilterProxy<TActionFilter> : BaseActionFilter
        where TActionFilter : BaseActionFilter
    {
        /// <summary>
        /// Resolves the action filter and invokes it.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="continuation">The continuation for the request.</param>
        /// <returns>The result of resolved filter's execute function.</returns>
        public override async Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            // Trigger the creation of the scope.
            // This ties object lifetime management to the individual request.
            var scope = actionContext.Request.GetDependencyScope();

            // Resolve the type using dependency injection so that the actual filter has the proper dependencies populated.
            var handler = scope.GetService(typeof(TActionFilter)) as TActionFilter;

            var response = await handler.ExecuteActionFilterAsync(actionContext, cancellationToken, continuation).ConfigureAwait(false);

            return response;
        }
    }
}