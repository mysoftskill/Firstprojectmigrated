namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using V2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// A base class that adds support for running PDMS client methods.
    /// </summary>
    /// <typeparam name="T">The response type for the client action represented by this <c>cmdlet</c>.</typeparam>
    public abstract class BaseServiceCmdlet<T> : BaseCmdlet<T, V2.IDataManagementClient> where T : class, IHttpResult
    {
        /// <summary>
        /// Get the client.
        /// </summary>
        /// <returns>The client.</returns>
        protected override V2.IDataManagementClient GetClient()
        {
            return ServiceCmdlet.DataManagementClient;
        }

        /// <summary>
        /// Creates the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        protected override RequestContext CreateRequestContext()
        {
            return ServiceCmdlet.CreateRequestContext();
        }
    }
}