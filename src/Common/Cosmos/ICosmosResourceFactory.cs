namespace Microsoft.PrivacyServices.Common.Cosmos
{
    using System.Threading.Tasks;

    public interface ICosmosResourceFactory
    {
        /// <summary>
        /// Create an Adls gen1 based cosmos client
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        ICosmosClient CreateCosmosAdlsClient(AdlsConfig config);

        /// <summary>
        /// Retrieve token to be used with Adls based cosmos client.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task<string> GetAppToken(AdlsConfig config);
    }
}
