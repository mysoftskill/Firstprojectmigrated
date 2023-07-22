namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

    /// <summary>
    /// A base class that adds support for running PDMS client methods.
    /// </summary>
    /// <typeparam name="T">The response type for the client action represented by this <c>cmdlet</c>.</typeparam>
    public abstract class BaseServiceTreeCmdlet<T> : BaseCmdlet<T, IServiceTreeClient> where T : class, IHttpResult
    {
        /// <summary>
        /// Get the client.
        /// </summary>
        /// <returns>The client.</returns>
        protected override IServiceTreeClient GetClient()
        {
            return ServiceTreeCmdlet.ServiceTreeClient;
        }

        /// <summary>
        /// Creates the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        protected override RequestContext CreateRequestContext()
        {
            return ServiceTreeCmdlet.CreateRequestContext();
        }
    }
}