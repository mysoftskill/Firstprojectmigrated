namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// History item controller.
    /// </summary>
    [ODataRoutePrefix("historyItems")]
    public class HistoryItemsV2Controller : ODataController
    {
        private readonly IHistoryItemReader reader;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItemsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The history item reader.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public HistoryItemsV2Controller(
            IHistoryItemReader reader,
            IMapper mapper)
        {
            this.reader = reader;
            this.mapper = mapper;
        }

        /// <summary>
        /// Reads all history items using the provided query options.
        /// </summary>
        /// <group>History items V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/historyItems</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the name property. Ex: $filter=name eq 'value' OR $filter=contains(name, '{value}').</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="HistoryItem"/>A collection of history items. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<HistoryItem> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<HistoryItem, Core.HistoryItem, Core.HistoryItemFilterCriteria>(
                    queryOptions,
                    (filter, propertyName, propertyValue, operatorKind) =>
                    {
                        switch (propertyName)
                        {
                            case "id":
                                filter.EntityId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                            case "updatedOn":
                                if (operatorKind == OperatorKind.GreaterThanOrEquals)
                                {
                                    filter.EntityUpdatedAfter = (DateTimeOffset)propertyValue;
                                }

                                if (operatorKind == OperatorKind.LessThanOrEquals)
                                {
                                    filter.EntityUpdatedBefore = (DateTimeOffset)propertyValue;
                                }
                                
                                break;
                        }
                    });

            var response = await EntityModule.GetAllAsync(
                ModelState,
                this.mapper,
                this.Request,
                queryOptions,
                filterCriteria,
                (filter) => this.reader.ReadByFiltersAsync(filter)).ConfigureAwait(false);

            return this.Ok(response);
        }
    }
}