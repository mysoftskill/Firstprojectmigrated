[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "Swagger documentation.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:ElementReturnValueMustBeDocumented", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AspNet.OData;
    using AspNet.OData.Query;
    using AspNet.OData.Routing;

    using AutoMapper;

    using DataAccess.Reader;
    using DataAccess.Writer;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// VariantRequest controller.
    /// </summary>
    [ODataRoutePrefix("variantRequests")]
    public class VariantRequestsV2Controller : ODataController
    {
        private readonly IVariantRequestReader reader;
        private readonly IVariantRequestWriter writer;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The variant request reader.</param>
        /// <param name="writer">The variant request writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public VariantRequestsV2Controller(
            IVariantRequestReader reader,
            IVariantRequestWriter writer,
            IMapper mapper)
        {
            this.reader = reader;
            this.writer = writer;
            this.mapper = mapper;
        }

        /// <summary>
        /// Creates a variant request.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/variantRequests</url>
        /// <requestType><see cref="VariantRequest"/>A variant request with the following fields provided: requestedVariants, variantRelationships.</requestType>
        /// <response code="200"><see cref="VariantRequest"/>The created variant request with service generated properties filled in (such as Id and ETag).</response>
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] VariantRequest value)
        {
            var response = await EntityModule.CreateAsync<Core.VariantRequest, VariantRequest>(
                ModelState,
                value,
                this.mapper,
                this.writer.CreateAsync).ConfigureAwait(false);

            return this.Created(response);
        }

        /// <summary>
        /// Updates a variant request. This uses replace semantics, so all fields must be provided even if they are unchanged.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>PUT</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/variantRequests('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the variant request to update.</pathParam>
        /// <requestType><see cref="VariantRequest"/>A variant request with all fields provided (tracking details can be excluded).</requestType>        
        /// <response code="200"><see cref="VariantRequest"/>The updated variant request.</response>
        [HttpPut]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Update([FromODataUri] string id, [FromBody] VariantRequest value)
        {
            var response = await EntityModule.UpdateAsync<Core.VariantRequest, VariantRequest>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.UpdateAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Approves a variant request by id.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded Variant request. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantRequests('{id}')/v2.approve</url>
        /// <pathParam name="id" required="true" type="string">The id of the Variant request to approve.</pathParam>
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
        /// Deletes a variant request by id.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded Variant request. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantRequests('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the Variant request to delete.</pathParam>
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
        /// Reads a variant request by id.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantRequests('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the Variant request to retrieve.</pathParam>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="VariantRequest"/>The Variant request if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<VariantRequest> queryOptions)
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
        /// Reads all variant requests using the provided query options.
        /// </summary>
        /// <group>VariantRequests V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantRequests</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the ownerId and deleteAgentId properties. Ex: $filter=ownerId eq '{id}'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="VariantRequest"/>A collection of Variant requests. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<VariantRequest> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<VariantRequest, Core.VariantRequest, Core.VariantRequestFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
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

        private static Core.ExpandOptions GetExpandOptions(ODataQueryOptions<VariantRequest> queryOptions)
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