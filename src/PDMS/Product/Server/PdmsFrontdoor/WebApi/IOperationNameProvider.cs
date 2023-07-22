namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    /// <summary>
    /// Defines methods to convert operations into friendly names.
    /// </summary>
    public interface IOperationNameProvider
    {
        /// <summary>
        /// Given an URI path and query, returns an operation name for it.        
        /// </summary>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="pathAndQuery">The path and query of an URI.</param>
        /// <returns>The operation name.</returns>
        OperationName GetFromPathAndQuery(string httpMethod, string pathAndQuery);
    }

    /// <summary>
    /// Contains information about an operation's name.
    /// </summary>
    public class OperationName
    {
        /// <summary>
        /// Gets or sets a friendly name for the operation.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this operation should appear in telemetry.
        /// </summary>
        public bool IncludeInTelemetry { get; set; }
    }

    /// <summary>
    /// A default implementation that does not convert any value to a friendly name.
    /// </summary>
    public class DefaultOperationNameProvider : IOperationNameProvider
    {
        /// <summary>
        /// Simply returns the given value as a friendly name.
        /// </summary>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="pathAndQuery">The path and query of an URI.</param>
        /// <returns>The operation name.</returns>
        public OperationName GetFromPathAndQuery(string httpMethod, string pathAndQuery)
        {
            return new OperationName { FriendlyName = pathAndQuery, IncludeInTelemetry = true };
        }
    }
}