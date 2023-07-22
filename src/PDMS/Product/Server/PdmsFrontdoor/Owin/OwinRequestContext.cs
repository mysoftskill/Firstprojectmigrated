namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;

    /// <summary>
    /// Uses the <see cref="Microsoft.Owin.IOwinContext" /> to pass data between message handlers.
    /// This class is ideal if you need the data to also be available in OWIN middleware.
    /// </summary>
    public class OwinRequestContext : IRequestContext
    {
        /// <summary>
        /// The request object.
        /// </summary>
        private readonly HttpRequestMessage requestMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwinRequestContext" /> class.
        /// </summary>
        /// <param name="requestMessage">The request object.</param>
        public OwinRequestContext(HttpRequestMessage requestMessage)
        {
            this.requestMessage = requestMessage;
        }

        /// <summary>
        /// Retrieve the stored value assigned to a specific key.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value object.</returns>
        public TValue Get<TValue>(string key)
        {
            return this.requestMessage.GetOwinContext().Get<TValue>(key);
        }

        /// <summary>
        /// Store a value associated to a specific key.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value object.</param>
        public void Set<TValue>(string key, TValue value)
        {
            this.requestMessage.GetOwinContext().Set<TValue>(key, value);
        }
    }

    /// <summary>
    /// Creates an <see cref="OwinRequestContext" /> from an http request message.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Factory has no logic.")]
    public class OwinRequestContextFactory : IRequestContextFactory
    {
        /// <summary>
        /// Creates an <see cref="OwinRequestContext" /> from an http request message.
        /// </summary>
        /// <param name="requestMessage">The request object.</param>
        /// <returns>The request context object.</returns>
        public IRequestContext Create(HttpRequestMessage requestMessage)
        {
            return new OwinRequestContext(requestMessage);
        }
    }
}
