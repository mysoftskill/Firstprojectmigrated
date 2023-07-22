namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;

    /// <summary>
    ///     Interface for client.
    /// </summary>
    public interface IKustoClient
    {
        /// <summary>
        ///     Gets a response representing the query on the given database.
        /// </summary>
        /// <param name="query">Query parameter.</param>
        /// <returns>Returns a response task.</returns>
        Task<IHttpResult<KustoResponse>> QueryAsync(string query);
    }
}
