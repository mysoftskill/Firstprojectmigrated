namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Defines the attributes used to set operation related info in log
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class DisallowPort80ActionFilterAttribute : FilterAttribute, IActionFilter
    {
        /// <summary>
        /// Executes the filter action asynchronously.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token assigned for this task.</param>
        /// <param name="continuation">The delegate function to continue after the action method is invoked.</param>
        /// <returns>The newly created task for this operation.</returns>
        public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(actionContext.Request.RequestUri.Scheme, "http"))
            {
                IncomingEvent.Current?.SetProperty("HttpNotAllowed", "true");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.UpgradeRequired));
            }

            return continuation();
        }
    }
}
