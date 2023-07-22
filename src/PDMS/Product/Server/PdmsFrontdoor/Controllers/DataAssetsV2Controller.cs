namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// DataAsset controller.
    /// </summary>
    [ODataRoutePrefix("dataAssets")]
    public class DataAssetsV2Controller : ODataController
    {
        private readonly IDataAssetReader reader;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAssetsV2Controller" /> class.
        /// </summary>
        /// <param name="reader">The data asset reader.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public DataAssetsV2Controller(
            IDataAssetReader reader,
            IMapper mapper)
        {
            this.reader = reader;
            this.mapper = mapper;
        }

        /// <summary>
        /// Reads all data assets using the provided query options.
        /// </summary>
        /// <param name="queryOptions">Any additional query options.</param>
        /// <param name="qualifier">The asset qualifier to search on.</param>
        /// <returns>The data assets.</returns>
        [HttpGet]
        [ODataRoute("v2.findByQualifier(qualifier={qualifier})")]
        public async Task<IHttpActionResult> ReadByFilters(ODataQueryOptions<DataAsset> queryOptions, [FromODataUri] string qualifier)
        {
            var filterCriteria = new Core.DataAssetFilterCriteria();

            filterCriteria.Initialize(queryOptions);

            var assetQualifier = MappingProfile.ConstructAssetQualifier("qualifier", qualifier);

            var response = await EntityModule.GetAllAsync(
                ModelState,
                this.mapper,
                this.Request,
                queryOptions,
                filterCriteria,
                (filter) => this.reader.FindByQualifierAsync(filter, assetQualifier, false)).ConfigureAwait(false);

            return this.Ok(response);
        }
    }
}
