namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    /// <summary>
    /// An OWIN middleware class that automatically instruments requests with success/failure information.
    /// The request duration calculated by the OWIN middleware is more accurate than any WebAPI handler.
    /// </summary>
    public class LogMiddleWare : OwinMiddleware
    {
        private readonly OwinMiddleware next;
        private readonly ISessionFactory sessionFactory;
        private readonly SessionProperties sessionProperties;
        private readonly IOperationNameProvider operationNameProvider;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IOwinConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMiddleWare" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the stack.</param>
        /// <param name="sessionFactory">The session factory to create a new session.</param>
        /// <param name="sessionProperties">The session properties for the entire request.</param>
        /// <param name="operationNameProvider">The operation name provider to determine the name of each request.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="config">Configuration for this component.</param>
        public LogMiddleWare(
            OwinMiddleware next,
            ISessionFactory sessionFactory,
            SessionProperties sessionProperties,
            IOperationNameProvider operationNameProvider,
            AuthenticatedPrincipal authenticatedPrincipal,
            IOwinConfiguration config) : base(next)
        {
            this.next = next;
            this.sessionFactory = sessionFactory;
            this.sessionProperties = sessionProperties;
            this.operationNameProvider = operationNameProvider;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.config = config;
        }

        /// <summary>
        /// Invokes the middleware behavior.
        /// Begins calculating request duration and then calls into the next layer in the stack.
        /// Afterwards, logs the success/failure result and calculated duration.
        /// Any unhandled exceptions are also logged as ServiceFaults.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>A task.</returns>
        public override async Task Invoke(IOwinContext context)
        {
            var operationName = this.operationNameProvider.GetFromPathAndQuery(context.Request.Method, context.Request.Uri.PathAndQuery);

            this.authenticatedPrincipal.OperationName = operationName.FriendlyName;

            if (operationName.IncludeInTelemetry)
            {
                var cv = context?.Request?.Headers?.Get("MS-CV");

                if (!string.IsNullOrEmpty(cv))
                {
                    // Must set this before the session is started.
                    this.sessionProperties.CV.Set(cv);
                }

                var cc = context?.Request?.Headers?.Get("Correlation-Context");

                if (!string.IsNullOrEmpty(cc))
                {
                    this.sessionProperties.CC = cc;
                }

                var session = this.sessionFactory.StartSession(operationName.FriendlyName, SessionType.Incoming);

                var operationMetadata = new OperationMetadata().FillForOwin(context.Request);

                try
                {
                    await this.next.Invoke(context).ConfigureAwait(false);
                    
                    operationMetadata.FillForOwin(context.Response);

                    if (context.Response.StatusCode < 400)
                    {
                        session.Success(operationMetadata);
                    }
                    else
                    {
                        // This should cover all exceptions from the WebApi layer
                        // because we have the ServiceExceptionHandler, which catches
                        // and stores all exceptions from WebApi into this context.
                        var exn = context.Get<ServiceException>(IRequestContextExtensions.ServiceExceptionKey);

                        if (exn != null)
                        {
                            var result = Tuple.Create(operationMetadata, exn.ServiceError);

                            if (context.Response.StatusCode < 500)
                            {
                                session.Error(result);
                            }
                            else
                            {
                                session.Fault(result);
                            }
                        }
                        else
                        {
                            if (context.Response.StatusCode < 500)
                            {
                                session.Error(operationMetadata);
                            }
                            else
                            {
                                session.Fault(operationMetadata);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Any exception caught at this point can only have come 
                    // from OWIN middlewares or the OWIN pipeline itself.
                    // As such, they are not likely due to any code issues on our side.
                    // As we identify unimportant errors that occur, we change them from
                    // ServiceFault to ServiceError to avoid QOS impact.
                    if (ex is OperationCanceledException)
                    {
                        var elapsed = session.Stop();

                        if (elapsed < this.config.MaximumCancelationThresholdMilliseconds)
                        {
                            var serviceError = new ServiceError("RequestCanceled", "The request was canceled by the caller.");
                            var result = Tuple.Create(operationMetadata, serviceError);
                            session.Error(result);
                        }
                        else
                        {
                            var serviceError = new ServiceError("RequestCanceled:LatencyThresholdExceeded", "The request was canceled by the caller and the latency of the service exceeded the expected max duration.");
                            var result = Tuple.Create(operationMetadata, serviceError);
                            session.Fault(result);
                        }
                    }
                    else if (ex is ObjectDisposedException)
                    {
                        var elapsed = session.Stop();

                        if (elapsed < this.config.MaximumCancelationThresholdMilliseconds)
                        {
                            var serviceError = new ServiceError("RequestCanceled:Disposed", "The request was disposed before a response could be sent.");
                            var result = Tuple.Create(operationMetadata, serviceError);
                            session.Error(result);
                        }
                        else
                        {
                            var serviceError = new ServiceError("RequestCanceled:LatencyThresholdExceeded:Disposed", "The request was disposed before a response could be sent and the latency of the service exceeded the expected max duration.");
                            var result = Tuple.Create(operationMetadata, serviceError);
                            session.Fault(result);
                        }
                    }
                    else
                    {
                        var serviceError = new ServiceFault(ex);
                        var result = Tuple.Create(operationMetadata, serviceError.ServiceError);
                        session.Fault(result);
                        throw;
                    }
                }
            }
            else
            {
                await this.next.Invoke(context).ConfigureAwait(false);
            }
        }
    }
}
