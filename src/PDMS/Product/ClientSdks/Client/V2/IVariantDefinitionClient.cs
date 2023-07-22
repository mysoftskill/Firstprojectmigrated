namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the variant definition controller.
    /// </summary>
    public interface IVariantDefinitionClient
    {
        /// <summary>
        /// Issues a create call for the given variant definition
        /// and returns the newly created variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant definition.</returns>
        Task<IHttpResult<VariantDefinition>> CreateAsync(VariantDefinition variantDefinition, RequestContext requestContext);

        /// <summary>
        /// Issues a read call for the given variant definition id.
        /// </summary>
        /// <param name="id">The id of the variant definition to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding variant definition.</returns>
        Task<IHttpResult<VariantDefinition>> ReadAsync(string id, RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known variant definitions.
        /// If the number of existing variant definitions is larger than the configured server-side max page size,
        /// then only the first page variant definitions are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant definition filter criteria.</param>
        /// <returns>A collection result with all the returned variant definitions, total number of existing variant definitions and possible next page link.</returns>
        Task<IHttpResult<Collection<VariantDefinition>>> ReadByFiltersAsync(RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None, VariantDefinitionFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known variant definitions. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant definition filter criteria.</param>
        /// <returns>All available variant definitions.</returns>
        Task<IHttpResult<IEnumerable<VariantDefinition>>> ReadAllByFiltersAsync(RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None, VariantDefinitionFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues an update call for the given variant definition
        /// and returns the updated variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant definition.</returns>
        Task<IHttpResult<VariantDefinition>> UpdateAsync(VariantDefinition variantDefinition, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call for the given variant definition id.
        /// </summary>
        /// <param name="id">The id of the variant definition to delete.</param>
        /// <param name="etag">The ETag of the variant definition.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="force">The force delete flag.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext, bool force = false);
    }
}