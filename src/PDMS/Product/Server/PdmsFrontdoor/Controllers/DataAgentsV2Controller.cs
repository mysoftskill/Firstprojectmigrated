namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// DataAgent controller.
    /// </summary>
    [ODataRoutePrefix("dataAgents")]
    public class DataAgentsV2Controller : ODataController
    {
        private readonly IDataAgentReader reader;
        private readonly IDeleteAgentReader deleteAgentReader;
        private readonly IDataAgentWriter writer;
        private readonly IMapper mapper;
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IList<Guid> DataAgentOwnershipSecurityGroupIds;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly ICachedActiveDirectory cachedActiveDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgentsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The data agent reader.</param>
        /// <param name="deleteAgentReader">The delete agent reader.</param>
        /// <param name="writer">The data agent writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public DataAgentsV2Controller(
            IDataAgentReader reader,
            IDeleteAgentReader deleteAgentReader,
            IDataAgentWriter writer,
            IDataOwnerReader ownerReader,
            IMapper mapper,
            ICoreConfiguration coreConfiguration,
            AuthenticatedPrincipal authenticatedPrincipal,
            ICachedActiveDirectory cachedActiveDirectory)
        {
            this.reader = reader;
            this.deleteAgentReader = deleteAgentReader;
            this.writer = writer;
            this.mapper = mapper;
            this.DataAgentOwnershipSecurityGroupIds = coreConfiguration.DataAgentOwnershipSecurityGroupIds.Select(x => new Guid(x)).ToList();
            this.dataOwnerReader = ownerReader;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.cachedActiveDirectory = cachedActiveDirectory;
        }

        /// <summary>
        /// Creates a data agent. Agent must of of type v2.DeleteAgent
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>POST</verb>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents</url>
        /// <requestType><see cref="DataAgent"/>A data agent with the following fields provided: qualifier, deleteAgentId, ownerId. It is not required to set both deleteAgentId and ownerId, but you must have at least one of them.</requestType>
        /// <response code="200"><see cref="DataAgent"/>The created data agent with service generated properties filled in (such as Id and ETag).</response>
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] DataAgent value)
        {
            var response = await EntityModule.CreateAsync<Core.DataAgent, DataAgent>(
                ModelState,
                value,
                this.mapper,
                this.writer.CreateAsync).ConfigureAwait(false);

            return this.Created(response);
        }

        /// <summary>
        /// Updates a data agent. This uses replace semantics, so all fields must be provided even if they are unchanged.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>PUT</verb>        
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to update.</pathParam>
        /// <requestType><see cref="DataAgent"/>A data agent with all fields provided (tracking details can be excluded).</requestType>        
        /// <response code="200"><see cref="DataAgent"/>The updated data agent.</response>
        [HttpPut]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Update([FromODataUri] string id, [FromBody] DataAgent value)
        {
            var response = await EntityModule.UpdateAsync<Core.DataAgent, DataAgent>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.UpdateAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Deletes a data agent by id.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. The header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded data agent. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to delete.</pathParam>
        /// <response code="204">Empty.</response>
        [HttpDelete]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Delete([FromODataUri] string id)
        {
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            await EntityModule.DeleteAsync(
                id,
                etag?.Tag,
                false,
                false,
                ModelState,
                this.mapper,
                this.writer.DeleteAsync).ConfigureAwait(false);

            return this.StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Deletes a data agent by id with override pending commands check.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded data agent. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')/v2.override</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to delete.</pathParam>
        /// <pathParam name="overridePendingCommands" required="true" type="string">Override pending commands flag.</pathParam>
        /// <response code="204">Empty.</response>
        [HttpDelete]
        [ODataRoute("('{id}')/v2.override")]
        public async Task<IHttpActionResult> DeleteWithOverridePendingCommandsCheck([FromODataUri] string id)
        {
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            await EntityModule.DeleteAsync(
                id,
                etag?.Tag,
                true,
                false,
                ModelState,
                this.mapper,
                this.writer.DeleteAsync).ConfigureAwait(false);

            return this.StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Reads a data agent by id.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>     
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>   
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to retrieve.</pathParam>  
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="DataAgent"/>The data agent if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<DataAgent> queryOptions)
        {
            var expandOptions = GetExpandOptions(queryOptions);

            var response = await EntityModule.GetAsync(
                id,
                ModelState,
                this.mapper,
                queryOptions,
                (v) => this.reader.ReadByIdAsync(v, expandOptions)).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Reads a data agent by id and casts it into a v2.DeleteAgent. This API is required if you want to use $select for properties that are available only for delete agents.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')/v2.DeleteAgent</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to retrieve.</pathParam>  
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="DeleteAgent"/>The data agent if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')/v2.DeleteAgent")]
        public async Task<IHttpActionResult> ReadDeleteAgentById([FromODataUri] string id, ODataQueryOptions<DeleteAgent> queryOptions)
        {
            var expandOptions = GetExpandOptions(queryOptions);

            var response = await EntityModule.GetAsync(
                id,
                ModelState,
                this.mapper,
                queryOptions,
                (v) => this.deleteAgentReader.ReadByIdAsync(v, expandOptions)).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Calculates the registration status for a DeleteAgent.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{id}')/v2.DeleteAgent/v2.calculateRegistrationStatus</url>
        /// <pathParam name="id" required="true" type="string">The id of the data agent to retrieve.</pathParam>          
        /// <response code="200"><see cref="AgentRegistrationStatus"/>The agent registration status.</response>
        [HttpGet]
        [ODataRoute("('{id}')/v2.DeleteAgent/v2.calculateRegistrationStatus")]
        public async Task<IHttpActionResult> CalculateRegistrationStatus([FromODataUri] string id)
        {
            var response = await EntityModule.GetAsync(
                id,
                ModelState,
                this.mapper,
                (ODataQueryOptions<AgentRegistrationStatus>)null,
                (v) => this.deleteAgentReader.CalculateRegistrationStatus(v)).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Reads all data agents using the provided query options.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the ownerId and name properties. Ex: $filter=ownerId eq '{id}'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="DataAgent"/>A collection of data agents. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadDataAgentsByFilters(ODataQueryOptions<DataAgent> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<DataAgent, Core.DataAgent, Core.DataAgentFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "name":
                                filter.Name = new StringFilter((string)propertyValue, this.mapper.Map<StringComparisonType>(operatorKind));
                                break;
                            case "ownerId":
                                filter.OwnerId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                        }
                    });

            var expandOptions = GetExpandOptions(queryOptions);

            var response = await EntityModule.GetAllAsync(
                ModelState,
                this.mapper,
                this.Request,
                queryOptions,
                filterCriteria,
                (filter) => this.reader.ReadByFiltersAsync(filter, expandOptions)).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Reads all data agents or the DeleteAgent subtype using the provided query options.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/deleteAgents/v2.DeleteAgent</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the ownerId and name properties. Ex: $filter=ownerId eq '{id}'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="DeleteAgent"/>A collection of delete agents. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("v2.DeleteAgent")]
        public async Task<IHttpActionResult> ReadDeleteAgentsByFilters(ODataQueryOptions<DeleteAgent> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<DeleteAgent, Core.DeleteAgent, Core.DeleteAgentFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "name":
                                filter.Name = new StringFilter((string)propertyValue, this.mapper.Map<StringComparisonType>(operatorKind));
                                break;
                            case "ownerId":
                                filter.OwnerId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                            case "sharingEnabled":
                                filter.SharingEnabled = (bool)propertyValue;
                                break;
                        }
                    });

            var expandOptions = GetExpandOptions(queryOptions);

            var response = await EntityModule.GetAllAsync(
                ModelState,
                this.mapper,
                this.Request,
                queryOptions,
                filterCriteria,
                (filter) => this.deleteAgentReader.ReadByFiltersAsync(filter, expandOptions)).ConfigureAwait(false);

            return this.Ok(response);
        }


        /// <summary>
        /// Reads a user by id and returns OK if the user is the owner of the agent passed or if user is part MEEPrivacyService team
        /// </summary>
        /// <group>Users V2</group>
        /// <verb>GET</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataAgents('{agentId}')/v2.DeleteAgent/v2.checkOwnership</url>  
        /// <pathParam name="agentId" required="true" type="string">id of the agent.</pathParam>
        /// <response code="200"></response>
        [HttpGet]
        [ODataRoute("('{agentId}')/v2.DeleteAgent/v2.checkOwnership")]
        public async Task<IHttpActionResult> CheckOwnership([FromODataUri] string agentId)
        {
            var mySecurityGroupIds = await this.cachedActiveDirectory.GetSecurityGroupIdsAsync(this.authenticatedPrincipal).ConfigureAwait(false);
            var dataOwners = await this.dataOwnerReader.FindByAuthenticatedUserAsync(Core.ExpandOptions.ServiceTree).ConfigureAwait(false);
            
            var dataOwnerIds = dataOwners.Select(s => s.Id);

            var agentInfo = await EntityModule.GetAsync(
                agentId,
                ModelState,
                this.mapper,
                (ODataQueryOptions<DeleteAgent>)null,
                (v) => this.deleteAgentReader.ReadByIdAsync(v, Core.ExpandOptions.ServiceTree)).ConfigureAwait(false);

            if (dataOwnerIds.Contains(new Guid(agentInfo.OwnerId)) || mySecurityGroupIds.Intersect(this.DataAgentOwnershipSecurityGroupIds).Count()>0)
            {
                return this.Ok();
            }

            return this.StatusCode(System.Net.HttpStatusCode.Forbidden);
        }

        private static Core.ExpandOptions GetExpandOptions<TDataAgent>(ODataQueryOptions<TDataAgent> queryOptions)
            where TDataAgent : DataAgent
        {
            var expandOptions = Core.ExpandOptions.None;

            if (queryOptions?.SelectExpand != null)
            {
                var rawSelect = queryOptions.SelectExpand.RawSelect;

                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawSelect))
                {
                    if (rawSelect.Contains("trackingDetails") || rawSelect.Equals("*"))
                    {
                        expandOptions |= Core.ExpandOptions.TrackingDetails;
                    }

                    if (rawSelect.Contains("hasSharingRequests") || rawSelect.Equals("*"))
                    {
                        expandOptions |= Core.ExpandOptions.HasSharingRequests;
                    }
                }
            }

            return expandOptions;
        }
    }
}
