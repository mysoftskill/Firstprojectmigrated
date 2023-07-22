namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;
    using Microsoft.PrivacyServices.DataManagement.DataGridService;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Provides methods for reading data assets.
    /// </summary>
    public class DataAssetReader : IDataAssetReader
    {
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly ISessionFactory sessionFactory;
        private readonly IAuthorizationProvider authorizationProvider;
        private readonly AuthorizationRole authorizationRoles;
        private readonly IEventWriterFactory eventWriterFactory;

        private readonly IDataAssetProvider dataAssetProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAssetReader" /> class.
        /// </summary>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="dataAssetProvider">The DataAssetProvider instance to call datagrid</param>
        /// <param name="authenticatedPrincipal">The authenticated user.</param>
        /// <param name="sessionFactory">The session factory instance.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        public DataAssetReader(
            IAuthorizationProvider authorizationProvider,
            IDataAssetProvider dataAssetProvider,

            AuthenticatedPrincipal authenticatedPrincipal,
            ISessionFactory sessionFactory,
            IEventWriterFactory eventWriterFactory)
        {
            this.dataAssetProvider = dataAssetProvider;

            this.authorizationProvider = authorizationProvider;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.authorizationRoles = AuthorizationRole.ApplicationAccess;
            this.sessionFactory = sessionFactory;
            this.eventWriterFactory = eventWriterFactory;
        }

        /// <summary>
        /// Find data assets based on the asset qualifier.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the data asset.</param>
        /// <param name="qualifier">The asset qualifier to search on.</param>
        /// <param name="includeTags">Whether or not tag information should be retrieved.</param>
        /// <returns>Data assets matching the qualifier.</returns>
        public async Task<FilterResult<DataAsset>> FindByQualifierAsync(DataAssetFilterCriteria filterCriteria,
            AssetQualifier qualifier, bool includeTags)
        {
            await this.authorizationProvider.AuthorizeAsync(this.authorizationRoles).ConfigureAwait(false);

            (DataAccessResult logInfo, FilterResult<DataAsset> dataAssets) result = (null, null);
            try
            {
                result = await this.sessionFactory.InstrumentAsync(
                    "DataGrid.FindDataAssetsByQualifierAsync",
                    SessionType.Outgoing,
                    async () =>
                    {
                        var dataAssets = await this.dataAssetProvider.FindDataAssetsByQualifierAsync(filterCriteria, qualifier, includeTags);

                        return (new DataAccessResult()
                                {
                                    AccessKey = qualifier.ToString(),
                                    TotalHits = dataAssets.Count
                                }, dataAssets);
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Authorization has been denied for this request.") ||
                    ex.Message.Contains("User does not have"))
                {
                    throw new MissingWritePermissionException(this.authenticatedPrincipal.UserAlias,
                        AuthorizationRole.ApplicationAccess.ToString(),
                        "User does not have read permissions for DataGrid.");
                }
                else
                {
                    throw;
                }
            }

            return result.dataAssets;
        }
    }
}