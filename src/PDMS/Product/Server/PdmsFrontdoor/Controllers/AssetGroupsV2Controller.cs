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
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Identity.Metadata;

    using Core = Models.V2;

    /// <summary>
    /// AssetGroup controller.
    /// </summary>
    [ODataRoutePrefix("assetGroups")]
    public class AssetGroupsV2Controller : ODataController
    {
        private readonly IManifest identityManifest;
        private readonly IAssetGroupReader reader;
        private readonly IAssetGroupWriter writer;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The data owner reader.</param>
        /// <param name="writer">The data owner writer.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        /// <param name="identityManifest">The identify manifest instance.</param>
        public AssetGroupsV2Controller(
            IAssetGroupReader reader,
            IAssetGroupWriter writer,
            IMapper mapper,
            IManifest identityManifest)
        {
            this.reader = reader;
            this.writer = writer;
            this.mapper = mapper;
            this.identityManifest = identityManifest;
        }

        /// <summary>
        /// Creates an asset group.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups</url>
        /// <requestType><see cref="AssetGroup"/>An asset group with the following fields provided: qualifier, deleteAgentId, ownerId. It is not required to set both deleteAgentId and ownerId, but you must have at least one of them.</requestType>
        /// <response code="200"><see cref="AssetGroup"/>The created asset group with service generated properties filled in (such as Id and ETag).</response>
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] AssetGroup value)
        {
            var response = await EntityModule.CreateAsync<Core.AssetGroup, AssetGroup>(
                ModelState,
                value,
                this.mapper,
                this.writer.CreateAsync).ConfigureAwait(false);

            return this.Created(response);
        }

        /// <summary>
        /// Updates an asset group. This uses replace semantics, so all fields must be provided even if they are unchanged.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>PUT</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the asset group to update.</pathParam>
        /// <requestType><see cref="AssetGroup"/>An asset group with all fields provided (tracking details can be excluded).</requestType>        
        /// <response code="200"><see cref="AssetGroup"/>The updated asset group.</response>
        [HttpPut]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> Update([FromODataUri] string id, [FromBody] AssetGroup value)
        {
            var response = await EntityModule.UpdateAsync<Core.AssetGroup, AssetGroup>(
                id,
                ModelState,
                value,
                this.mapper,
                this.writer.UpdateAsync).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Deletes an asset group by id.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>DELETE</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="If-Match" required="true" type="string">The ETag of the previously downloaded asset group. A non matching value will result in a failure.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the asset group to delete.</pathParam>
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
        /// Reads an asset group by id.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header> 
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups('{id}')</url>
        /// <pathParam name="id" required="true" type="string">The id of the asset group to retrieve.</pathParam>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <response code="200"><see cref="AssetGroup"/>The asset group if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id, ODataQueryOptions<AssetGroup> queryOptions)
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
        /// Reads all asset groups using the provided query options.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>GET</verb>    
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups</url>
        /// <queryParam name="$select" required="false" type="string">A select clause for the query. If provided, then only the requested properties are returned. If not provided, then all properties are returned.</queryParam>
        /// <queryParam name="$filter" required="false" type="string">A filter clause for the query. Filters are supported for the ownerId and deleteAgentId properties. Ex: $filter=ownerId eq '{id}'.</queryParam>
        /// <queryParam name="$top" required="false" type="int">Determines page size for the request. If not provided, then server side paging will be used.</queryParam>
        /// <queryParam name="$skip" required="false" type="int">Determines page index for the request. If not provided, then the first page is returned.</queryParam>
        /// <response code="200"><see cref="PageResult{T}"/> where T is <see cref="AssetGroup"/>A collection of asset groups. If server-side paging is triggered, than the nextLink property will be set. Use that to download the next page.</response>
        [HttpGet]
        [ODataRoute("")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<AssetGroup> queryOptions)
        {
            var filterCriteria = FilterCriteriaModule.Create<AssetGroup, Core.AssetGroup, Core.AssetGroupFilterCriteria>(
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
                            case "exportAgentId":
                                filter.ExportAgentId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                            case "accountCloseAgentId":
                                filter.AccountCloseAgentId = MappingProfile.ConstructGuid(propertyName, propertyValue as string);
                                break;
                            case "qualifier":
                                var value = MappingProfile.ConstructAssetQualifier(propertyName, propertyValue as string);
                                filter.Qualifier = this.GetQualifierFilterCriteria(value, operatorKind);
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
        /// API always returns isCompliant = true state
        /// Changes to this API are being made as part of Decoupling from cosmos 
        /// After discussion from Scott, we can return IsCompliant = true and deprecate the AssetGroupWorker
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>GET</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups/v2.findByAssetQualifier(qualifier={qualifier})/complianceState</url>        
        /// <pathParam name="qualifier" required="true" type="string">The fully specified asset qualifier to search on. Use the identity library to create valid qualifiers.</pathParam>
        /// <response code="200"><see cref="ComplianceState"/>The ComplianceState is always compliant.</response>
        [HttpGet]
        [ODataRoute("v2.findByAssetQualifier(qualifier={qualifier})/complianceState")]
        public async Task<IHttpActionResult> GetComplianceStateByAssetQualifier([FromODataUri] string qualifier)
        {
            return await Task.FromResult(this.Ok(new ComplianceState { IsCompliant = true, IncompliantReason = null }));
        }

        /// <summary>
        /// Link/unlink asset groups to agents or create/delete requests for linking (based on ownership).
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups/v2.setAgentRelationships</url>
        /// <requestType><see cref="SetAgentRelationshipParameters"/>The set of relationships and actions to apply.</requestType>
        /// <response code="200"><see cref="SetAgentRelationshipResponse"/>The results of each releationship action.</response>
        [HttpPost]
        [ODataRoute("v2.setAgentRelationships")]
        public async Task<IHttpActionResult> SetAgentRelationships(ODataActionParameters actionParameters)
        {
            var relationships = actionParameters["relationships"] as IEnumerable<SetAgentRelationshipParameters.Relationship>;
            var apiRequest = new SetAgentRelationshipParameters { Relationships = relationships };

            if (!ModelState.IsValid)
            {
                throw new InvalidModelError(ModelState);
            }

            try
            {
                var coreRequest = this.mapper.Map<Models.V2.SetAgentRelationshipParameters>(apiRequest);

                var coreResponse = await this.writer.SetAgentRelationshipsAsync(coreRequest).ConfigureAwait(false);

                var apiResponse = EntityModule.Map<Models.V2.SetAgentRelationshipResponse, SetAgentRelationshipResponse>(this.mapper, coreResponse);

                return this.Ok(apiResponse);
            }
            catch (CoreException coreException)
            {
                throw this.mapper.Map<ServiceException>(coreException);
            }
        }

        /// <summary>
        /// Remove/unlink variants from the given asset group.
        /// </summary>
        /// <group>AssetGroups V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups('{id}')/v2.removeVariants</url>
        /// <pathParam name="id" required="true" type="string">The id of the asset group.</pathParam>
        /// <requestType>
        /// <see cref="RemoveVariantsParameters"/>Action parameter that contains a list of variant ids that should be removed from the asset group.
        /// </requestType>
        /// <response code="200"><see cref="AssetGroup"/>The updated asset group with the variants removed.</response>
        [HttpPost]
        [ODataRoute("('{id}')/v2.removeVariants")]
        public async Task<IHttpActionResult> RemoveVariants([FromODataUri] string id, ODataActionParameters actionParameters)
        {
            var variantIds = actionParameters["variantIds"];
            var etag = this.Request.Headers.IfMatch.FirstOrDefault();

            var newIds = (IEnumerable<string>)variantIds;

            var response = await EntityModule.ExecuteAsync<Core.AssetGroup, AssetGroup>(
                ModelState,
                this.mapper,
                () => this.writer.RemoveVariantsAsync(
                    MappingProfile.ConstructGuid("id", id),
                    newIds.Select(x => MappingProfile.ConstructGuid("variantIds", x)),
                    etag?.Tag)).ConfigureAwait(false);

            return this.Ok(response);
        }

        /// <summary>
        /// Calculates the registration status for an AssetGroup.
        /// </summary>
        /// <group>DataAgents V2</group>
        /// <verb>GET</verb>        
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/assetGroups('{id}')/v2.calculateRegistrationStatus</url>
        /// <pathParam name="id" required="true" type="string">The id of the asset group to retrieve.</pathParam>          
        /// <response code="200"><see cref="AssetGroupRegistrationStatus"/>The asset group registration status.</response>
        [HttpGet]
        [ODataRoute("('{id}')/v2.calculateRegistrationStatus")]
        public async Task<IHttpActionResult> CalculateRegistrationStatus([FromODataUri] string id)
        {
            var response = await EntityModule.GetAsync(
                id,
                ModelState,
                this.mapper,
                (ODataQueryOptions<AssetGroupRegistrationStatus>)null,
                (v) => this.reader.CalculateRegistrationStatus(v)).ConfigureAwait(false);

            return this.Ok(response);
        }

        private static Core.ExpandOptions GetExpandOptions(ODataQueryOptions<AssetGroup> queryOptions)
        {
            var expandOptions = Core.ExpandOptions.None;

            if (queryOptions?.SelectExpand != null)
            {
                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawSelect) &&
                    queryOptions.SelectExpand.RawSelect.Contains("trackingDetails"))
                {
                    expandOptions |= Core.ExpandOptions.TrackingDetails;
                }

                if (!string.IsNullOrEmpty(queryOptions.SelectExpand.RawExpand))
                {
                    if (queryOptions.SelectExpand.RawExpand.Contains("deleteAgent"))
                    {
                        expandOptions |= Core.ExpandOptions.DeleteAgent;
                    }

                    if (queryOptions.SelectExpand.RawExpand.Contains("exportAgent"))
                    {
                        expandOptions |= Core.ExpandOptions.ExportAgent;
                    }

                    if (queryOptions.SelectExpand.RawExpand.Contains("accountCloseAgent"))
                    {
                        expandOptions |= Core.ExpandOptions.AccountCloseAgent;
                    }

                    if (queryOptions.SelectExpand.RawExpand.Contains("inventory"))
                    {
                        expandOptions |= Core.ExpandOptions.Inventory;
                    }

                    if (queryOptions.SelectExpand.RawExpand.Contains("dataAssets"))
                    {
                        expandOptions |= Core.ExpandOptions.DataAssets;
                    }
                }
            }

            return expandOptions;
        }

        // This method supports two different OperatorKinds: Equals and Contains.
        // Equals means: Find all asset groups with this exact qualifier string.
        // Contains means: Find all asset groups that have a qualifier contained by (or equal to) this qualifier string.
        private IDictionary<string, StringFilter> GetQualifierFilterCriteria(AssetQualifier qualifier, OperatorKind operatorKind)
        {
            var typeDefinition = this.identityManifest.AssetTypes.Single(x => x.Id == qualifier.AssetType);

            Func<string, string> getValue = (propName) =>
                qualifier.Properties.ContainsKey(propName) ? qualifier.Properties[propName] : null;

            Func<string, StringComparisonType> getComparision = (propName) =>
            {
                var propDefinition = typeDefinition.Properties.Single(x => x.Id == propName);

                if (operatorKind == OperatorKind.Contains && propDefinition.PartialMatch)
                {
                    return propDefinition.CaseSensitive ? StringComparisonType.StartsWithCaseSensitive : StringComparisonType.StartsWith;
                }
                else
                {
                    return propDefinition.CaseSensitive ? StringComparisonType.EqualsCaseSensitive : StringComparisonType.Equals;
                }
            };

            // Assume Contains by default.
            if (operatorKind == OperatorKind.Equals)
            {
                // Must check every possible property for Equals match.
                return typeDefinition.Properties.ToDictionary(
                        v => v.Id,
                        v => new StringFilter(getValue(v.Id), getComparision(v.Id)));
            }
            else
            {
                // Only check the properties given for a Contains match.
                return qualifier.Properties.Keys.Where(x => x != "AssetType").ToDictionary(
                        id => id,
                        id => new StringFilter(getValue(id), getComparision(id)));
            }
        }
    }
}