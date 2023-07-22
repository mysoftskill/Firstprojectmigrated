namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Exposes the available APIs for the service that target the AssetGroup controller.
    /// </summary>
    public interface IAssetGroupClient
    {
        /// <summary>
        /// Determines if there is a compliant asset group for the given asset qualifier.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="BadArgumentError.InvalidArgument">
        /// Thrown when the qualifier is not fully specified.
        /// </exception>
        /// <exception cref="NotFoundError">
        /// Thrown when no asset group can be found for the given qualifier.
        /// </exception>
        /// <param name="qualifier">The asset qualifier upon which to search.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The compliance state.</returns>
        Task<IHttpResult<ComplianceState>> GetComplianceStateByAssetQualifierAsync(AssetQualifier qualifier, RequestContext requestContext);

        /// <summary>
        /// Issues a create call for the given asset group
        /// and returns the newly created asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued asset group.</returns>
        Task<IHttpResult<AssetGroup>> CreateAsync(AssetGroup assetGroup, RequestContext requestContext);

        /// <summary>
        /// Issues a read call for the given asset group id.
        /// </summary>
        /// <param name="id">The id of the asset group to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding asset group.</returns>
        Task<IHttpResult<AssetGroup>> ReadAsync(string id, RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known asset groups.
        /// If the number of existing asset groups is larger than the configured server-side max page size,
        /// then only the first page asset groups are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The asset group filter criteria.</param>
        /// <returns>A collection result with all the returned asset groups, total number of existing asset groups and possible next page link.</returns>
        Task<IHttpResult<Collection<AssetGroup>>> ReadByFiltersAsync(RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None, AssetGroupFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known asset groups. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The asset group filter criteria.</param>
        /// <returns>All available asset groups.</returns>
        Task<IHttpResult<IEnumerable<AssetGroup>>> ReadAllByFiltersAsync(RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None, AssetGroupFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues an update call for the given asset group
        /// and returns the updated asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued asset group.</returns>
        Task<IHttpResult<AssetGroup>> UpdateAsync(AssetGroup assetGroup, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call with the given asset group id.
        /// </summary>
        /// <param name="id">The id of the asset group to delete.</param>
        /// <param name="etag">The ETag of the asset group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext);

        /// <summary>
        /// Set agent relationships in bulk. Creates requests as needed.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The response.</returns>
        Task<IHttpResult<SetAgentRelationshipResponse>> SetAgentRelationshipsAsync(SetAgentRelationshipParameters parameters, RequestContext requestContext);

        /// <summary>
        /// Invokes the calculate agent registration status API for asset groups.
        /// </summary>
        /// <param name="id">The id of the asset group to use in the query.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The registration status.</returns>
        Task<IHttpResult<AssetGroupRegistrationStatus>> CalculateRegistrationStatus(string id, RequestContext requestContext);

        /// <summary>
        /// Removes a set of variants from an asset group.
        /// </summary>
        /// <param name="id">The id of the asset group to delete.</param>
        /// <param name="variantIds">The list of variant ids to remove from the asset group.</param>
        /// <param name="etag">The ETag of the asset group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult<AssetGroup>> RemoveVariantsAsync(string id, string[] variantIds, string etag, RequestContext requestContext);
    }
}