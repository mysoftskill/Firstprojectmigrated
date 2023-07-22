namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a standard set of call patterns for interacting with an http service.
    /// </summary>
    public interface IHttpServiceProxy
    {
        /// <summary>
        /// Issues a GET call.
        /// </summary>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult{TResponse}" /></returns>
        Task<IHttpResult<TResponse>> GetAsync<TResponse>(string url, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken);

        /// <summary>
        /// Issues a POST call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult{TResponse}" /></returns>
        Task<IHttpResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest payload, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken);

        /// <summary>
        /// Issues a DELETE call.
        /// </summary>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        Task<IHttpResult> DeleteAsync<TRequest>(string url, TRequest payload, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken);

        /// <summary>
        /// Issues a DELETE call.
        /// </summary>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        Task<IHttpResult> DeleteAsync(string url, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken);

        /// <summary>
        /// Issues a PUT call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        Task<IHttpResult<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest payload, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken);
    }
}