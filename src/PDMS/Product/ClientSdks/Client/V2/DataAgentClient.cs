namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataAgentClient controller.
    /// </summary>
    internal class DataAgentClient : IDataAgentClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgentClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public DataAgentClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a create call for the given data agent
        /// and returns the newly created data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data agent.</returns>
        public async Task<IHttpResult<TDataAgent>> CreateAsync<TDataAgent>(TDataAgent dataAgent, RequestContext requestContext) where TDataAgent : DataAgent
        {
            var result =
                await this.httpServiceProxy.PostAsync<TDataAgent, TDataAgent>(
                    "/api/v2/dataAgents",
                    dataAgent,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given data agent id.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="id">The id of the data agent to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding data agent.</returns>
        public async Task<IHttpResult<TDataAgent>> ReadAsync<TDataAgent>(string id, RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None) where TDataAgent : DataAgent
        {
            string url = $"/api/v2/dataAgents('{id}'){GetDataAgentExpandOptions<TDataAgent>(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<TDataAgent>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known data agents.
        /// If the number of existing data agents is larger than the configured server-side max page size,
        /// then only the first page data agents are returned.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data agent filter criteria.</param>
        /// <returns>A collection result with all the returned data agents, total number of existing data agents and possible next page link.</returns>
        public async Task<IHttpResult<Collection<TDataAgent>>> ReadByFiltersAsync<TDataAgent>(RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None, DataAgentFilterCriteria<TDataAgent> filterCriteria = null)
            where TDataAgent : DataAgent
        {
            string url = $"/api/v2/dataAgents{GetDataAgentExpandOptions<TDataAgent>(expandOptions)}{GetDataAgentFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<TDataAgent>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known data agents. Automatically handles any service side paging.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data agent filter criteria.</param>
        /// <returns>All available data agents.</returns>
        public Task<IHttpResult<IEnumerable<TDataAgent>>> ReadAllByFiltersAsync<TDataAgent>(RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None, DataAgentFilterCriteria<TDataAgent> filterCriteria = null)
            where TDataAgent : DataAgent
        {
            return DataManagementClient.ReadMany<TDataAgent>(
                $"/api/v2/dataAgents{GetDataAgentExpandOptions<TDataAgent>(expandOptions)}{GetDataAgentFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues an update call for the given data agent
        /// and returns the updated data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data agent.</returns>
        public async Task<IHttpResult<TDataAgent>> UpdateAsync<TDataAgent>(TDataAgent dataAgent, RequestContext requestContext)
            where TDataAgent : DataAgent
        {
            var result =
                await this.httpServiceProxy.PutAsync<TDataAgent, TDataAgent>(
                    $"/api/v2/dataAgents('{dataAgent.Id}')",
                    dataAgent,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Calls the api to check the agent ownership
        /// </summary>
        /// <param name="id">The data agent to check.</param>
        /// <param name="requestContext">The request context.</param>
        public async Task<IHttpResult> CheckOwnership(string id, RequestContext requestContext)
        {
            string url = $"/api/v2/dataAgents('{id}')/v2.DeleteAgent/v2.checkOwnership";

            var result =
                await this.httpServiceProxy.GetAsync<IHttpResult>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a delete call with the given data agent id.
        /// </summary>
        /// <param name="id">The id of the data agent to delete.</param>
        /// <param name="etag">The ETag of the data agent.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="overridePendingCommandsCheck">The Override flag for pending commands check.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext, bool overridePendingCommandsCheck = false)
        {
            var headers = requestContext.GetHeaders();
            if (!string.IsNullOrWhiteSpace(etag))
            {
                headers.Add("If-Match", () => Task.FromResult(etag));
            }

            IHttpResult result = overridePendingCommandsCheck
                ? await this.httpServiceProxy.DeleteAsync(
                        $"/api/v2/dataAgents('{id}')/v2.override",
                        headers,
                        requestContext.CancellationToken).ConfigureAwait(false)
                : await this.httpServiceProxy.DeleteAsync(
                        $"/api/v2/dataAgents('{id}')",
                        headers,
                        requestContext.CancellationToken).ConfigureAwait(false);
            return result.Get(2);
        }

        /// <summary>
        /// Invokes the calculate agent registration status API for delete agents.
        /// </summary>
        /// <param name="id">The id of the delete agent to use in the query.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The registration status.</returns>
        public async Task<IHttpResult<AgentRegistrationStatus>> CalculateDeleteAgentRegistrationStatus(string id, RequestContext requestContext)
        {
            string url = $"/api/v2/dataAgents('{id}')/v2.DeleteAgent/v2.calculateRegistrationStatus";

            var result =
                await this.httpServiceProxy.GetAsync<AgentRegistrationStatus>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }


        /// <summary>
        /// Helper function to convert OperationalReadinessLow and OperationalReadinessHigh to boolean array.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">DataAgent object.</param>
        /// <returns>Boolean array representing operational readiness.</returns>
        public bool[] GetOperationalReadinessBooleanArray<TDataAgent>(TDataAgent dataAgent)
             where TDataAgent : DataAgent
        {
            return ConvertLongToBooleanArray(dataAgent.OperationalReadinessLow).Concat(ConvertLongToBooleanArray(dataAgent.OperationalReadinessHigh)).ToArray();
        }

        /// <summary>
        /// Helper function to convert boolean array to OperationalReadinessLow and OperationalReadinessHigh in data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent object.</param>
        /// <param name="operationalReadiness">The OperationalReadiness boolean array.</param>
        public void SetOperationalReadiness<TDataAgent>(TDataAgent dataAgent, bool[] operationalReadiness)
            where TDataAgent : DataAgent
        {
            int sizeOfLong = sizeof(long) * 8;

            if (operationalReadiness.Length != sizeOfLong * 2)
            {
                throw new ArgumentException($"Boolean array size {operationalReadiness.Length} is not correct. Should be size of " + (sizeOfLong * 2), nameof(operationalReadiness));
            }

            dataAgent.OperationalReadinessLow = ConvertBooleanArrayToLong(operationalReadiness.Take(sizeOfLong).ToArray());
            dataAgent.OperationalReadinessHigh = ConvertBooleanArrayToLong(operationalReadiness.Skip(sizeOfLong).Take(sizeOfLong).ToArray());
        }

        /// <summary>
        /// Get filter criteria to be used in url from DataAgentFilterCriteria.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="filterCriteria">Data agent filter criteria.</param>
        /// <returns>Data agent filter criteria in string format.</returns>
        private static string GetDataAgentFilterCriteria<TDataAgent>(DataAgentFilterCriteria<TDataAgent> filterCriteria)
            where TDataAgent : DataAgent
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }

        /// <summary>
        /// Get expand options to be used in url from AssetGroupExpandOptions.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="expandOptions">Asset group expand options.</param>
        /// <returns>Asset group expand options in string format.</returns>
        private static string GetDataAgentExpandOptions<TDataAgent>(DataAgentExpandOptions expandOptions) where TDataAgent : DataAgent
        {
            string queryString;

            var agentType = typeof(TDataAgent);

            if (agentType == typeof(DeleteAgent))
            {
                queryString = $"/v2.DeleteAgent?$select=id,eTag,name,description,connectionDetails,migratingConnectionDetails,ownerId,capabilities,operationalReadinessLow,operationalReadinessHigh,icm,inProdDate,sharingEnabled,isThirdPartyAgent,deploymentLocation,supportedClouds,dataResidencyBoundary";
            }
            else
            {
                // We cannot perform specific selection using DataAgent generically.
                // If we do, then we risk returning a subset of properties,
                // and that breaks our Replace semantics for updates.
                return "?$select=*";
            }

            if (expandOptions != DataAgentExpandOptions.None)
            {
                if (expandOptions.HasFlag(DataAgentExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                if (expandOptions.HasFlag(DataAgentExpandOptions.HasSharingRequests) && agentType == typeof(DeleteAgent))
                {
                    queryString += ",hasSharingRequests";
                }
            }

            return queryString;
        }

        private static bool[] ConvertLongToBooleanArray(long number)
        {
            int length = sizeof(long) * 8;
            bool[] result = new bool[length];

            for (int index = 0; index < length; index++)
            {
                if ((number & 1L) > 0)
                {
                    result[index] = true;
                }

                number >>= 1;
            }

            return result;
        }

        private static long ConvertBooleanArrayToLong(bool[] operationalReadiness)
        {
            long result = 0;
            long mask = 1;

            foreach (var readiness in operationalReadiness)
            {
                if (readiness)
                {
                    result |= mask;
                }

                mask <<= 1;
            }

            return result;
        }
    }
}