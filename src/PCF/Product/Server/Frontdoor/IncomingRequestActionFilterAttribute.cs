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

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Defines the attributes used to set operation related info in log
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class IncomingRequestActionFilterAttribute : FilterAttribute, IActionFilter
    {
        private readonly string componentName;
        private readonly string operationName;
        private readonly string operationVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncomingRequestActionFilterAttribute"/> class.
        /// </summary>
        /// <param name="component">Component name.</param>
        /// <param name="operationName">Name of the incoming request operation name.</param>
        /// <param name="operationVersion">Version of the operation.</param>
        public IncomingRequestActionFilterAttribute(string component, string operationName, string operationVersion)
        {
            this.componentName = component;
            this.operationName = operationName;
            this.operationVersion = operationVersion;
        }

        /// <summary>
        /// Executes the filter action asynchronously.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token assigned for this task.</param>
        /// <param name="continuation">The delegate function to continue after the action method is invoked.</param>
        /// <returns>The newly created task for this operation.</returns>
        public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            return Logger.InstrumentAsync(new IncomingEvent(SourceLocation.Here()), InstrumentContinuationAsync);

            async Task<HttpResponseMessage> InstrumentContinuationAsync(IncomingEvent @event)
            {
                @event.OperationName = $"{this.componentName}.{this.operationName}.{this.operationVersion}";
                @event.TargetUri = actionContext.Request.RequestUri.AbsoluteUri;
                @event.RequestMethod = actionContext.Request.Method.Method;
                @event.CallerIpAddress = actionContext.Request.GetOwinContext().Request.RemoteIpAddress;

                if (actionContext.Request.Headers.TryGetValues("x-client-version", out IEnumerable<string> clientVersionValues))
                {
                    @event.SetProperty("ClientVersion", clientVersionValues.FirstOrDefault());
                }

                if (actionContext.Request.Headers.TryGetValues("x-lease-duration-seconds", out IEnumerable<string> requestedLeaseDuration))
                {
                    @event.SetProperty("RequestedLeaseDuration", requestedLeaseDuration.FirstOrDefault());
                }

                HttpResponseMessage response = null;
                try
                {
                    response = await continuation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Any exception that bubbles this far is a 500 error. Hopefully everything
                    // is being handled correctly in business logic.
                    response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    @event.OperationStatus = OperationStatus.UnexpectedFailure;

                    @event.SetException(ex);
                    Logger.Instance?.UnexpectedException(ex);
                }
                finally
                {
                    @event.StatusCode = response.StatusCode;
                }

                // Add a metadata header to indicate that this machine served this request.
                response.Headers.Add("X-Served-By", EnvironmentInfo.NodeName);
                return response;
            }
        }
    }
}
