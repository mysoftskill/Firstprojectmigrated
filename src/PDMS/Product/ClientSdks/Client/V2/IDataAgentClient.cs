namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataAgent controller.
    /// </summary>
    public interface IDataAgentClient
    {
        /// <summary>
        /// Issues a create call for the given data agent
        /// and returns the newly created data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data agent.</returns>
        Task<IHttpResult<TDataAgent>> CreateAsync<TDataAgent>(TDataAgent dataAgent, RequestContext requestContext) where TDataAgent : DataAgent;

        /// <summary>
        /// Issues a read call for the given data agent id.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="id">The id of the data agent to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding data agent.</returns>
        Task<IHttpResult<TDataAgent>> ReadAsync<TDataAgent>(string id, RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None) where TDataAgent : DataAgent;

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
        Task<IHttpResult<Collection<TDataAgent>>> ReadByFiltersAsync<TDataAgent>(RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None, DataAgentFilterCriteria<TDataAgent> filterCriteria = null) where TDataAgent : DataAgent;

        /// <summary>
        /// Issues a read call that retrieves all known data agents. Automatically handles any service side paging.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data agent filter criteria.</param>
        /// <returns>All available data agents.</returns>
        Task<IHttpResult<IEnumerable<TDataAgent>>> ReadAllByFiltersAsync<TDataAgent>(RequestContext requestContext, DataAgentExpandOptions expandOptions = DataAgentExpandOptions.None, DataAgentFilterCriteria<TDataAgent> filterCriteria = null) where TDataAgent : DataAgent;

        /// <summary>
        /// Issues an update call for the given data agent
        /// and returns the updated data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data agent.</returns>
        Task<IHttpResult<TDataAgent>> UpdateAsync<TDataAgent>(TDataAgent dataAgent, RequestContext requestContext) where TDataAgent : DataAgent;

        /// <summary>
        /// Issues a delete call with the given data agent id.
        /// </summary>
        /// <param name="id">The id of the data agent to delete.</param>
        /// <param name="etag">The ETag of the data agent.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="overridePendingCommandsCheck">The Override flag for pending commands check.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext, bool overridePendingCommandsCheck = false);


        /// <summary>
        /// Calls the api to check the agent ownership
        /// </summary>
        /// <param name="id">The data agent to check.</param>
        /// <param name="requestContext">The request context.</param>
        Task<IHttpResult> CheckOwnership(string id, RequestContext requestContext);

        /// <summary>
        /// Invokes the calculate agent registration status API for delete agents.
        /// </summary>
        /// <param name="id">The id of the delete agent to use in the query.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The registration status.</returns>
        Task<IHttpResult<AgentRegistrationStatus>> CalculateDeleteAgentRegistrationStatus(string id, RequestContext requestContext);

        /// <summary>
        /// Helper function to convert OperationalReadinessLow and OperationalReadinessHigh to boolean array.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent object.</param>
        /// <returns>Boolean array representing operational readiness.</returns>
        bool[] GetOperationalReadinessBooleanArray<TDataAgent>(TDataAgent dataAgent) where TDataAgent : DataAgent;

        /// <summary>
        /// Helper function to convert boolean array to OperationalReadinessLow and OperationalReadinessHigh in data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent object.</param>
        /// <param name="operationalReadiness">The OperationalReadiness boolean array.</param>
        void SetOperationalReadiness<TDataAgent>(TDataAgent dataAgent, bool[] operationalReadiness) where TDataAgent : DataAgent;
    }
}