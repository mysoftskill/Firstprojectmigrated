[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "Swagger documentation.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:ElementReturnValueMustBeDocumented", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AspNet.OData;
    using AspNet.OData.Query;
    using AspNet.OData.Routing;

    using AutoMapper;

    using DataAccess.Reader;
    using DataAccess.Writer;
    using Microsoft.OData;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Variant definition controller.
    /// </summary>
    [ODataRoutePrefix("variantDefinitions")]
    public class VariantDefinitionsV2Controller : ODataController
    {
        private readonly IVariantDefinitionReader reader;
        private readonly IVariantDefinitionWriter writer;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantDefinitionsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The variant definition reader.</param>
        /// <param name="writer">The variant definition writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public VariantDefinitionsV2Controller(
            IVariantDefinitionReader reader,
            IVariantDefinitionWriter writer,
            IMapper mapper)
        {
            this.reader = reader;
            this.writer = writer;
            this.mapper = mapper;
        }

        /// <summary>
        /// Creates a variant definition.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions</url>
        /// <requestType><see cref="VariantDefinition"/>a variant definition with the following fields provided: name, description.</requestType>
        /// <response code="200"><see cref="VariantDefinition"/>The created variant definition with service generated properties filled in (such as Id and ETag).</response>
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] VariantDefinition value)
        {
            var response = await EntityModule.CreateAsync<Core.VariantDefinition, VariantDefinition>(
                ModelState,
                value,
                this.mapper,
                this.writer.CreateAsync).ConfigureAwait(false);

            return this.Created(response);
        }

        /// <summary>
        /// Updates a variant definition. This uses replace semantics, so all fields must be provided even if they are unchanged.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>PUT</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the variant definition to update.</pathParam>
        /// <requestType><see cref="VariantDefinition"/>A variant definition with all fields provided (tracking details can be excluded).</requestType>        
        /// <response code="200"><see cref="VariantDefinition"/>The updated variant definition.</response>
        [HttpPut]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Update([FromODataUri] string id, [FromBody] VariantDefinition value)
        {
            var response = await EntityModule.UpdateAsync<Core.VariantDefinition, VariantDefinition>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.UpdateAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Reads a variant definition by id.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the variant definition to retrieve.</pathParam>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="VariantDefinition"/>The variant definition if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<VariantDefinition> queryOptions)
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
        /// Reads all variant definitions using the provided query options.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the name and state property. Ex: $filter=name eq 'value' OR $filter=contains(name, '{value}') or $filter=state eq 'Closed'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="VariantDefinition"/>A collection of variant definitions. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<VariantDefinition> queryOptions)
        {
            IFilterCriteria<Core.VariantDefinition> filterCriteria = null;
            try
            {
                filterCriteria = FilterCriteriaModule.Create<VariantDefinition, Core.VariantDefinition, Core.VariantDefinitionFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "name":
                                filter.Name = new StringFilter((string)propertyValue, this.mapper.Map<StringComparisonType>(operatorKind));
                                break;
                            case "state":
                                Core.VariantDefinitionState state;
                                if (Enum.TryParse((propertyValue as ODataEnumValue)?.Value, true, out state))
                                {
                                    filter.State = state;
                                }
                                break;
                        }
                    });
            }
            catch (ODataException ex)
            {
                throw new InvalidArgumentError("filterCriteria", ex.Message);
            }

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
        /// Delete a variant definition by id.
        /// Deletion will fail if any other entities are linked to the given variant definition.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded variant definition. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the variant definition to update.</pathParam>
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
        /// Delete a variant definition by id.
        /// All other entities linked to the given variant will be unlinked.
        /// </summary>
        /// <group>Variant definitions V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded variant definition. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/VariantDefinitions('{id}')/force</url>
        /// <pathParam name="id" required="true" type="string">The id of the variant definition to update.</pathParam>
        /// <response code="204">Empty.</response>
        [HttpDelete]
        [ODataRoute("('{id}')/force")]
        public async Task<IHttpActionResult> ForceDelete([FromODataUri] string id)
        {
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            await EntityModule.DeleteAsync(
                id,
                etag?.Tag,
                false,
                true,
                ModelState,
                this.mapper,
                this.writer.DeleteAsync).ConfigureAwait(false);

            return this.StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        private static Core.ExpandOptions GetExpandOptions(ODataQueryOptions<VariantDefinition> queryOptions)
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