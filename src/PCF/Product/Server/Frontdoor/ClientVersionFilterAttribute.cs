namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    /// Client version filter attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ClientVersionFilterAttribute : FilterAttribute, IActionFilter
    {
        // todo(yoshyo): Use flighting to define the value below
        internal const string MinimumPcfClientVersion = "1.1.18102.5";

        /// <summary>
        /// Executes the filter action asynchronously.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token assigned for this task.</param>
        /// <param name="continuation">The delegate function to continue after the action method is invoked.</param>
        /// <returns>The newly created task for this operation.</returns>
        public Task<HttpResponseMessage> ExecuteActionFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            if (EnvironmentInfo.IsDevBoxEnvironment && !EnvironmentInfo.IsUnitTest)
            {
                return continuation();
            }

            string clientVersion = ClientVersionHelper.GetClientVersionNumber(actionContext.Request);
            if (string.IsNullOrWhiteSpace(clientVersion))
            {
                // non-sdk users may fall in this case, so we fail open
                return continuation();
            }
            else if (string.Compare(clientVersion, MinimumPcfClientVersion, StringComparison.Ordinal) >= 0)
            {
                // Accept good version
                return continuation();
            }

            // Reject outdated client call
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"Bad request: Your pcf sdk client ({clientVersion}) is outdated. Please upgrade it to version {MinimumPcfClientVersion} or higher.")
            };

            return Task.FromResult(response);
        }
    }
}
