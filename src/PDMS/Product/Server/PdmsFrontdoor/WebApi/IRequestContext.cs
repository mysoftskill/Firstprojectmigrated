namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Net.Http;

    /// <summary>
    /// Provides a bridge between message handlers.
    /// </summary>
    public interface IRequestContext
    {
        /// <summary>
        /// Store a value associated to a specific key.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value object.</param>
        void Set<TValue>(string key, TValue value);

        /// <summary>
        /// Retrieve the stored value assigned to a specific key.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value object.</returns>
        TValue Get<TValue>(string key);
    }

    /// <summary>
    /// Creates a request context from an http request message.
    /// This allows different request context objects for IIS and OWIN.
    /// This is particularly important if you want to access the data from OWIN middleware.
    /// </summary>
    public interface IRequestContextFactory
    {
        /// <summary>
        /// Creates a request context from an http request message.
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <returns>The request context.</returns>
        IRequestContext Create(HttpRequestMessage requestMessage);
    }
}
