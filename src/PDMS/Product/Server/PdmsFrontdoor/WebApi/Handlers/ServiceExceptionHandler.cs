namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Web.Http.ExceptionHandling;
    using System.Web.Http.Results;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// An exception handler that captures all exceptions and stores them on the request context.
    /// This is necessary so that the log handler can extract the exception information and log it.
    /// </summary>
    public class ServiceExceptionHandler : ExceptionHandler
    {
        /// <summary>
        ///  We must provide a formatter for custom serialization of the error response.
        ///  We must use a custom approach because the OData infrastructure will fail if you try to return the error normally.
        /// </summary>
        private static readonly IContractResolver ContractResolver = new CamelCasePropertyNamesContractResolver();
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = ContractResolver, NullValueHandling = NullValueHandling.Ignore };
        private static readonly MediaTypeFormatter Formatter = new JsonMediaTypeFormatter() { SerializerSettings = SerializerSettings };

        /// <summary>
        /// The request context factory.
        /// </summary>
        private readonly IRequestContextFactory contextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceExceptionHandler" /> class.
        /// </summary>
        /// <param name="contextFactory">The request context factory.</param>
        public ServiceExceptionHandler(IRequestContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// Extracts the exception information and stores it on the request context.
        /// If the exception was a not a ServiceException, then it is assumed to be an unhandled exception.
        /// </summary>
        /// <param name="context">The exception context.</param>
        public override void Handle(ExceptionHandlerContext context)
        {
            var serviceException = context.Exception as ServiceException;
            if (serviceException == null)
            {
                serviceException = new ServiceFault(context.Exception);
            }

            this.contextFactory.Create(context.Request).SetServiceException(serviceException);
                        
            var response = new HttpResponseMessage(serviceException.StatusCode);
            response.Content = new ObjectContent<ResponseError>(new ResponseError { Error = serviceException.ServiceError }, Formatter);

            context.Result = new ResponseMessageResult(response);
        }
    }
}
