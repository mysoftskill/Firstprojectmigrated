namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    /// <summary>
    /// Base filter implementation.
    /// </summary>
    public abstract class BaseActionFilter : IActionFilter
    {
        /// <summary>
        /// Gets a value indicating whether or not the filter can be used multiple times.
        /// </summary>
        public virtual bool AllowMultiple
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Performs some behavior before/after the controller action is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="continuation">The continuation for the request.</param>
        /// <returns>A response.</returns>
        public abstract Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation);
    }
}