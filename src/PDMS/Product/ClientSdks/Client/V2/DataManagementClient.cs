namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service.
    /// </summary>
    public class DataManagementClient : IDataManagementClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataManagementClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public DataManagementClient(IHttpServiceProxy httpServiceProxy)
        {
            this.DataOwners = new DataOwnerClient(httpServiceProxy);
            this.AssetGroups = new AssetGroupClient(httpServiceProxy);
            this.VariantDefinitions = new VariantDefinitionClient(httpServiceProxy);
            this.DataAgents = new DataAgentClient(httpServiceProxy);
            this.DataAssets = new DataAssetClient(httpServiceProxy);
            this.Users = new UserClient(httpServiceProxy);
            this.HistoryItems = new HistoryItemClient(httpServiceProxy);
            this.SharingRequests = new SharingRequestClient(httpServiceProxy);
            this.VariantRequests = new VariantRequestClient(httpServiceProxy);
            this.Incidents = new IncidentClient(httpServiceProxy);
            this.TransferRequests = new TransferRequestClient(httpServiceProxy);
        }

        /// <summary>
        /// Gets the data owner client.
        /// </summary>
        public IDataOwnerClient DataOwners { get; private set; }

        /// <summary>
        /// Gets the asset group client.
        /// </summary>
        public IAssetGroupClient AssetGroups { get; private set; }

        /// <summary>
        /// Gets the variant definition client.
        /// </summary>
        public IVariantDefinitionClient VariantDefinitions { get; private set; }

        /// <summary>
        /// Gets the data agent client.
        /// </summary>
        public IDataAgentClient DataAgents { get; private set; }

        /// <summary>
        /// Gets the data asset client.
        /// </summary>
        public IDataAssetClient DataAssets { get; private set; }

        /// <summary>
        /// Gets the user client.
        /// </summary>
        public IUserClient Users { get; private set; }

        /// <summary>
        /// Gets the history item client.
        /// </summary>
        public IHistoryItemClient HistoryItems { get; private set; }

        /// <summary>
        /// Gets the sharing request client.
        /// </summary>
        public ISharingRequestClient SharingRequests { get; private set; }

        /// <summary>
        /// Gets the variant request client.
        /// </summary>
        public IVariantRequestClient VariantRequests { get; private set; }

        /// <summary>
        /// Gets the incident client.
        /// </summary>
        public IIncidentClient Incidents { get; private set; }

        /// <summary>
        /// Gets the transfer request client.
        /// </summary>
        public ITransferRequestClient TransferRequests { get; private set; }

        /// <summary>
        /// Reads many items from the service. If server-side paging is triggered, then continues reading batches until all requested items are received.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="path">The request path.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        /// <returns>The results.</returns>
        public static async Task<IHttpResult<IEnumerable<T>>> ReadMany<T>(string path, RequestContext requestContext, IHttpServiceProxy httpServiceProxy)
        {
            var response = Enumerable.Empty<T>();

            IHttpResult<Collection<T>> result;

            do
            {
                result = await httpServiceProxy.GetAsync<Collection<T>>(
                    path,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

                response = response.Concat(result.Get(2).Response.Value);

                path = result.Response.NextLink?.PathAndQuery;
            }
            while (result.Response.NextLink != null);

            return result.Convert(_ => response, 2);
        }
    }
}