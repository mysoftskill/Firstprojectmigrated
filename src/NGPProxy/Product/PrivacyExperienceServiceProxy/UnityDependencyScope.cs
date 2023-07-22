// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Web.Http.Dependencies;

    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Unity DependencyScope
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UnityDependencyScope : IDependencyScope
    {
        private const string ComponentName = nameof(UnityDependencyScope);

        private const string NamespacePrefix = "Microsoft.Membership.MemberServices";

        // These types are allowed to be ignored during dependency resolving.
        private readonly IList<string> AllowableIgnoredTypes = new List<string>
        {
            "System.Web.Http.Metadata.ModelMetadataProvider",
            "System.Web.Http.Tracing.ITraceManager",
            "System.Web.Http.Tracing.ITraceWriter",
            "System.Web.Http.Dispatcher.IHttpControllerSelector",
            "System.Web.Http.Dispatcher.IAssembliesResolver",
            "System.Web.Http.Dispatcher.IHttpControllerActivator",
            "System.Web.Http.Dispatcher.IHttpControllerTypeResolver",
            "System.Web.Http.Controllers.IActionValueBinder",
            "System.Web.Http.Controllers.IHttpActionInvoker",
            "System.Net.Http.Formatting.IContentNegotiator",
            "System.Web.Http.Controllers.IHttpActionSelector",
            "System.Web.Http.Validation.IBodyModelValidator",
            "System.Web.Http.Hosting.IHostBufferPolicySelector",
            "System.Web.Http.ExceptionHandling.IExceptionHandler"
        };

        private readonly IUnityContainer container;

        private readonly ILogger logger;

        /// <summary>
        ///     Determines <see cref="Dispose(bool)" /> has already been called.
        /// </summary>
        private bool disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnityDependencyScope" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="logger">The logger.</param>
        public UnityDependencyScope(IUnityContainer container, ILogger logger)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Implement <see cref="IDisposable" />; dispose resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Retrieves a service from the scope.
        /// </summary>
        /// <param name="serviceType">The service to be retrieved.</param>
        /// <returns>
        ///     The retrieved service.
        /// </returns>
        public object GetService(Type serviceType)
        {
            object service = null;

            try
            {
                service = this.container.Resolve(serviceType);
            }
            catch (ResolutionFailedException ex)
            {
                this.HandleResolutionFailure(ex);
            }

            return service;
        }

        /// <summary>
        ///     Retrieves a collection of services from the scope.
        /// </summary>
        /// <param name="serviceType">The collection of services to be retrieved.</param>
        /// <returns>
        ///     The retrieved collection of services.
        /// </returns>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            IEnumerable<object> services = null;

            try
            {
                services = this.container.ResolveAll(serviceType);
            }
            catch (ResolutionFailedException ex)
            {
                this.HandleResolutionFailure(ex);
            }

            return services;
        }

        /// <summary>
        ///     Dispose underlying AP resources.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="System.IDisposable" />; otherwise false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Nothing to dispose.
            }

            this.disposed = true;
        }

        /// <summary>
        ///     Handles a resolution failure by logging a message at the appropriate level. Only failures related to the
        ///     project's namespace will be logged as errors.
        /// </summary>
        /// <param name="ex">The resolution failed exception to handle.</param>
        private void HandleResolutionFailure(ResolutionFailedException ex)
        {
            const string Message = "Unity resolution failed: {0}";
            const string InnerExceptionMessageFormat = "The current type, {0}";

            if (ex.InnerException != null &&
                ex.InnerException.Source.StartsWith(NamespacePrefix, StringComparison.OrdinalIgnoreCase))
            {
                this.logger.Error(ComponentName, Message, ex);
            }
            else if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message) &&
                     this.AllowableIgnoredTypes.Any(type => ex.InnerException.Message.StartsWith(string.Format(InnerExceptionMessageFormat, type))))
            {
                this.logger.Verbose(ComponentName, $"Ignoring {nameof(ResolutionFailedException)} for {nameof(ex.TypeRequested)}: {ex.TypeRequested}");
            }
            else
            {
                this.logger.Warning(ComponentName, Message, ex);
            }
        }
    }
}
