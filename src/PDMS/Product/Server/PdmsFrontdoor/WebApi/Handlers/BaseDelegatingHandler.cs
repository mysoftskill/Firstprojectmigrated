namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides some base functionality for all delegating handlers.
    /// In particular, this provides some test hooks for unit testing the handlers.
    /// </summary>
    public abstract class BaseDelegatingHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDelegatingHandler" /> class.
        /// </summary>
        public BaseDelegatingHandler()
        {
            this.BaseSendAsync = base.SendAsync;
        }

        /// <summary>
        /// This is a wrapper around the base.SendAsync function. This is a test hook.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The response object.</returns>
        protected delegate Task<HttpResponseMessage> SendAsyncHandler(HttpRequestMessage request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets or sets the base.SendAsync method. This is a test hook.
        /// </summary>
        protected SendAsyncHandler BaseSendAsync { get; set; }

        /// <summary>
        /// Calls the inner handler's message handling function.
        /// This exists so that it is clear that base.SendAsync above
        /// is calling the underlying SendAsync instead of the version tied to this handler.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The inner response.</returns>
        protected abstract override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}