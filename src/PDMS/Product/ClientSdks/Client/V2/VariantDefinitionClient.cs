namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the variant definition controller.
    /// </summary>
    internal class VariantDefinitionClient : IVariantDefinitionClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantDefinitionClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public VariantDefinitionClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a create call for the given variant definition
        /// and returns the newly created variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant definition.</returns>
        public async Task<IHttpResult<VariantDefinition>> CreateAsync(VariantDefinition variantDefinition, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<VariantDefinition, VariantDefinition>(
                    "/api/v2/variantDefinitions",
                    variantDefinition,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given variant definition id.
        /// </summary>
        /// <param name="id">The id of the variant definition to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding variant definition.</returns>
        public async Task<IHttpResult<VariantDefinition>> ReadAsync(string id, RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None)
        {
            string url = $"/api/v2/variantDefinitions('{id}'){GetVariantDefinitionExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<VariantDefinition>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known variant definitions.
        /// If the number of existing variant definitions is larger than the configured server-side max page size,
        /// then only the first page variant definitions are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant definition filter criteria.</param>
        /// <returns>A collection result with all the returned variant definitions, total number of existing variant definitions and possible next page link.</returns>
        public async Task<IHttpResult<Collection<VariantDefinition>>> ReadByFiltersAsync(RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None, VariantDefinitionFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/variantDefinitions{GetVariantDefinitionExpandOptions(expandOptions)}{GetVariantDefinitionFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<VariantDefinition>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known variant definitions. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant definition filter criteria.</param>
        /// <returns>All available variant definitions.</returns>
        public Task<IHttpResult<IEnumerable<VariantDefinition>>> ReadAllByFiltersAsync(RequestContext requestContext, VariantDefinitionExpandOptions expandOptions = VariantDefinitionExpandOptions.None, VariantDefinitionFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<VariantDefinition>(
                $"/api/v2/variantDefinitions{GetVariantDefinitionExpandOptions(expandOptions)}{GetVariantDefinitionFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues an update call for the given variant definition
        /// and returns the updated variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant definition.</returns>
        public async Task<IHttpResult<VariantDefinition>> UpdateAsync(VariantDefinition variantDefinition, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PutAsync<VariantDefinition, VariantDefinition>(
                    $"/api/v2/variantDefinitions('{variantDefinition.Id}')",
                    variantDefinition,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <inheritdoc/>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                throw new ArgumentNullException(nameof(etag));
            }

            var headers = requestContext.GetHeaders();
            headers.Add("If-Match", () => Task.FromResult(etag));

            var url = $"/api/v2/variantDefinitions('{id}')";
            if (force)
            {
                url += "/force";
            }

            var result =
                await this.httpServiceProxy.DeleteAsync(
                    url,
                    headers,
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Get expand options to be used in url from VariantDefinitionExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Variant definition expand options.</param>
        /// <returns>Variant definition expand options in string format.</returns>
        private static string GetVariantDefinitionExpandOptions(VariantDefinitionExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason";

            if (expandOptions == VariantDefinitionExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                if (expandOptions.HasFlag(VariantDefinitionExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from VariantDefinitionFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Variant definition filter criteria.</param>
        /// <returns>Variant definition filter criteria in string format.</returns>
        private static string GetVariantDefinitionFilterCriteria(VariantDefinitionFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}