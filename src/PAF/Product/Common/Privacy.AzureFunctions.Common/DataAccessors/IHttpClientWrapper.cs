namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a Wrapper for HttpClient
    /// </summary>
    public interface IHttpClientWrapper
    {
        /// <summary>
        /// Calls a protected Web API
        /// </summary>
        /// <param name="apiUrl">Url of the Web API to call (supposed to return Json)</param>
        /// <param name="accessTokenFunc">Function to provide access token used as a bearer security token to call the Web API</param>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<T> GetAsync<T>(string apiUrl, Func<Task<string>> accessTokenFunc);

        /// <summary>
        /// Update a protected Web API
        /// </summary>
        /// <param name="httpMethod">HttpMethod to use for the update.</param>
        /// <param name="apiUrl">Url of the Web API to call (supposed to return Json)</param>
        /// <param name="accessTokenFunc">Function to provide access token used as a bearer security token to call the Web API</param>
        /// <param name="payload">The content to be updated.</param>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<T> UpdateAsync<T>(HttpMethod httpMethod, string apiUrl, Func<Task<string>> accessTokenFunc, T payload);

        /// <summary>
        /// Post change to a protected Web API
        /// </summary>
        /// <param name="apiUrl">Url of the Web API to call returns empty response</param>
        /// <param name="accessTokenFunc">Function to provide access token used as a bearer security token to call the Web API</param>
        /// <param name="etag">Etag of the variant request</param>
        /// <returns>A <see cref="Task"/> that returns the httpresponse</returns>
        Task<HttpResponseMessage> PostAsync(string apiUrl, Func<Task<string>> accessTokenFunc, string etag);

        /// <summary>
        /// Delete a protected Web API and return the web response
        /// </summary>
        /// <param name="apiUrl">Url of the Web API to call</param>
        /// <param name="accessTokenFunc">Function to provide access token used as a bearer security token to call the Web API</param>
        /// <param name="etag">Etag of the variant request</param>
        /// <returns>A <see cref="Task"/> that returns the httpresponse</returns>
        Task<HttpResponseMessage> DeleteAsync(string apiUrl, Func<Task<string>> accessTokenFunc, string etag);
    }
}
