namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
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

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// SharingRequest controller.
    /// </summary>
    [ODataRoutePrefix("sharingRequests")]
    public class SharingRequestsV2Controller : ODataController
    {
        private readonly ISharingRequestReader reader;
        private readonly ISharingRequestWriter writer;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharingRequestsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The data owner reader.</param>
        /// <param name="writer">The data owner writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public SharingRequestsV2Controller(
            ISharingRequestReader reader,
            ISharingRequestWriter writer,
            IMapper mapper)
        {
            this.reader = reader;
            this.writer = writer;
            this.mapper = mapper;
        }

        /// <summary>
        /// Approves a sharing request by id.
        /// </summary>
        /// <group>SharingRequests V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded sharing request. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/sharingRequests('{id}')/v2.approve</url>
        /// <pathParam name="id" required="true" type="string">The id of the sharing request to approve.</pathParam>
        /// <response code="204">Empty.</response>
        [HttpPost]
        [ODataRoute("('{id}')/v2.approve")]
        public async Task<IHttpActionResult> Approve([FromODataUri] string id)
        {
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            // We use the delete method because it performs the correct set of validations
            // for an API with no input or output.
            await EntityModule.DeleteAsync(
                id,
                etag?.Tag,
                ModelState,
                this.mapper,
                this.writer.ApproveAsync).ConfigureAwait(false);

            return this.StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Deletes a sharing request by id.
        /// </summary>
        /// <group>SharingRequests V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded sharing request. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/sharingRequests('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the sharing request to delete.</pathParam>
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
        /// Reads a sharing request by id.
        /// </summary>
        /// <group>SharingRequests V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/sharingRequests('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the sharing request to retrieve.</pathParam>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="SharingRequest"/>The sharing request if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<SharingRequest> queryOptions)
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
        /// Reads all sharing requests using the provided query options.
        /// </summary>
        /// <group>SharingRequests V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/sharingRequests</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the ownerId and deleteAgentId properties. Ex: $filter=ownerId eq '{id}'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="SharingRequest"/>A collection of sharing requests. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<SharingRequest> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<SharingRequest, Core.SharingRequest, Core.SharingRequestFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "ownerId":
                                filter.OwnerId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                            case "deleteAgentId":
                                filter.DeleteAgentId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
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

        private static Core.ExpandOptions GetExpandOptions(ODataQueryOptions<SharingRequest> queryOptions)
        {
            var expandOptions = Core.ExpandOptions.None;

            if (queryOptions?.SelectExpand != null)
            {
                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawSelect) &&
                    queryOptions.SelectExpand.RawSelect.Contains("trackingDetails"))
                {
                    expandOptions |= Core.ExpandOptions.TrackingDetails;
                }
            }

            return expandOptions;
        }
    }
}