namespace Microsoft.PrivacyServices.DataManagement.Client
{
    /// <summary>
    /// A result from the service. Contains metadata about the call for instrumentation purposes.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    public interface IHttpResult<T> : IHttpResult
    {
        /// <summary>
        /// Gets the response as a strongly typed object.
        /// </summary>
        T Response { get; }
    }
}