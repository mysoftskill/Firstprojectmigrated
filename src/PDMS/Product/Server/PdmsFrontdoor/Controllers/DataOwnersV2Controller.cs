namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// DataOwner controller.
    /// </summary>
    [ODataRoutePrefix("dataOwners")]
    public class DataOwnersV2Controller : ODataController
    {
        private readonly IDataOwnerReader reader;
        private readonly IDataOwnerWriter writer;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnersV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The data owner reader.</param>
        /// <param name="writer">The data owner writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public DataOwnersV2Controller(
            IDataOwnerReader reader,
            IDataOwnerWriter writer,
            IMapper mapper)
        {
            this.reader = reader;
            this.writer = writer;
            this.mapper = mapper;
        }

        /// <summary>
        /// Creates a data owner.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners</url>  
        /// <requestType><see cref="DataOwner"/>A data owner with the following fields provided: alertContacts, announcementContacts, writeSecurityGroups, name, description.</requestType>
        /// <response code="200"><see cref="DataOwner"/>The created data owner with service generated properties filled in (such as Id and ETag).</response>        
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] DataOwner value)
        {
            var response = await EntityModule.CreateAsync<Core.DataOwner, DataOwner>(
                ModelState,
                value,
                this.mapper,
                this.writer.CreateAsync).ConfigureAwait(false);

            return this.Created(response);
        }

        /// <summary>
        /// Updates a data owner. This uses replace semantics, so all fields must be provided even if they are unchanged.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>PUT</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners('{id}')</url>  
        /// <pathParam name="id" required="true" type="string">The id of the data owner to update.</pathParam>
        /// <requestType><see cref="DataOwner"/>A data owner with all fields provided (tracking details can be excluded).</requestType>
        /// <response code="200"><see cref="DataOwner"/>The updated data owner.</response>
        [HttpPut]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Update([FromODataUri] string id, [FromBody] DataOwner value)
        {
            var response = await EntityModule.UpdateAsync<Core.DataOwner, DataOwner>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.UpdateAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Deletes a data owner by id.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded data owner. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the data owner to delete.</pathParam>
        /// <response code="204">Empty.</response>
        [HttpDelete]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Delete([FromODataUri] string id)
        {
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            await EntityModule.DeleteAsync(
                id,
                etag?.Tag,
                ModelState,
                this.mapper,
                this.writer.DeleteAsync).ConfigureAwait(false);

            return this.StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Reads a data owner by id.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>GET</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners('{id}')</url>  
        /// <pathParam name="id" required="true" type="string">The id of the data owner to retrieve.</pathParam>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="DataOwner"/>The data owner if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<DataOwner> queryOptions)
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
        /// Reads all data owners using the provided query options.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>GET</verb>   
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the name property. Ex: $filter=name eq 'value' OR $filter=contains(name, '{value}').</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="DataOwner"/> A collection of data owners. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<DataOwner> queryOptions)
        {
            Action<Core.DataOwnerFilterCriteria, object, OperatorKind, Action<StringFilter>> setFilter = (filter, propertyValue, operatorKind, action) =>
            {
                filter.ServiceTree = filter.ServiceTree ?? new Models.V2.ServiceTreeFilterCriteria();
                if (propertyValue == null)
                {
                    action.Invoke(new StringFilter(null, StringComparisonType.EqualsCaseSensitive));
                }
                else
                {
                    action.Invoke(new StringFilter((string)propertyValue, this.mapper.Map<StringComparisonType>(operatorKind)));
                }
            };

            var filterCriteria = FilterCriteriaModule.Create<DataOwner, Core.DataOwner, Core.DataOwnerFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "name":
                                filter.Name = new StringFilter((string)propertyValue, this.mapper.Map<StringComparisonType>(operatorKind));
                                break;
                            case "divisionId":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.DivisionId = m);
                                break;
                            case "divisionName":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.DivisionName = m);
                                break;
                            case "organizationId":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.OrganizationId = m);
                                break;
                            case "organizationName":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.OrganizationName = m);
                                break;
                            case "serviceGroupId":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.ServiceGroupId = m);
                                break;
                            case "serviceGroupName":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.ServiceGroupName = m);
                                break;
                            case "teamGroupId":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.TeamGroupId = m);
                                break;
                            case "teamGroupName":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.TeamGroupName = m);
                                break;
                            case "serviceId":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.ServiceId = m);
                                break;
                            case "serviceName":
                                setFilter(filter, propertyValue, operatorKind, m => filter.ServiceTree.ServiceName = m);
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
        /// Reads all data owners for the current authenticated user. It identifies the security groups for the user
        /// and then retrieves any data owner that contains any of the user's security groups.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>GET</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners/v2.findByAuthenticatedUser</url>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="DataOwner"/>A collection of data owners. Paging is not supported for this API, so all owners are returned.</response>
        [HttpGet]
        [ODataRoute("v2.findByAuthenticatedUser")]
        public async Task<IHttpActionResult> FindByAuthenticatedUser(ODataQueryOptions<DataOwner> queryOptions)
        {
            var expandOptions = GetExpandOptions(queryOptions);

            var response = await EntityModule.GetAllAsync(
                ModelState,
                this.mapper,
                this.Request,
                queryOptions,
                (Core.DataOwnerFilterCriteria)null,
                async (_) =>
                {
                    var values = await this.reader.FindByAuthenticatedUserAsync(expandOptions).ConfigureAwait(false);
                    var count = values.Count();

                    return new FilterResult<Core.DataOwner> // Simulate a collection response.
                    {
                        Count = count,
                        Index = 0,
                        Values = values,
                        Total = count
                    };
                }).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Finds the existing data owner that contains the given serviceTree.serviceId
        /// and replaces the serviceTree object for the provided data owner with the found values.
        /// It then deletes the original service tree data owner and returns the updated owner.
        /// </summary>
        /// <group>DataOwners V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/dataOwners('{id}')/v2.replaceServiceId</url>  
        /// <pathParam name="id" required="true" type="string">The id of the data owner to update.</pathParam>
        /// <requestType>
        /// <see cref="ReplaceServiceIdParameters"/>Action parameters that contain a data owner with all fields provided
        /// (tracking details can be excluded). The serviceTree.serviceId must be set to the id that should be replaced.
        /// </requestType>
        /// <response code="200"><see cref="DataOwner"/>The updated data owner with properties replaced by the existing service tree entity.</response>
        [HttpPost]
        [ODataRoute("('{id}')/v2.replaceServiceId")]
        public async Task<IHttpActionResult> ReplaceServiceId([FromODataUri] string id, ODataActionParameters actionParameters)
        {
            var value = actionParameters["value"] as DataOwner;

            var response = await EntityModule.UpdateAsync<Core.DataOwner, DataOwner>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.ReplaceServiceIdAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        private static Core.ExpandOptions GetExpandOptions(ODataQueryOptions<DataOwner> queryOptions)
        {
            var expandOptions = Core.ExpandOptions.None;

            if (queryOptions?.SelectExpand != null)
            {
                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawSelect))
                {
                    if (queryOptions.SelectExpand.RawSelect.Contains("trackingDetails"))
                    {
                        expandOptions |= Core.ExpandOptions.TrackingDetails;
                    }

                    if (queryOptions.SelectExpand.RawSelect.Contains("serviceTree"))
                    {
                        expandOptions |= Core.ExpandOptions.ServiceTree;
                    }
                }

                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawExpand))
                {
                    if (queryOptions.SelectExpand.RawExpand.Contains("dataAgents"))
                    {
                        expandOptions |= Core.ExpandOptions.DataAgents;
                    }

                    if (queryOptions.SelectExpand.RawExpand.Contains("assetGroups"))
                    {
                        expandOptions |= Core.ExpandOptions.AssetGroups;
                    }
                }
            }

            return expandOptions;
        }
    }
}
