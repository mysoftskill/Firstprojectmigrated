namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DataPlatform.DataDiscoveryService.Contracts;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Identity.Metadata;

    using Newtonsoft.Json.Linq;

    using AssetType = DataPlatform.DataDiscoveryService.Contracts.AssetType;

    public class DataAssetProvider : IDataAssetProvider
    {
        private readonly IDataDiscoveryClientFactory dataDiscoveryClientFactory;
        private readonly IDataGridConfiguration datagridConfiguration;
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IManifest identityManifest;
        private readonly ISessionFactory sessionFactory;

        private readonly int defaultDataAssetPageSize;
        private readonly int maxDataAssetPageSize;

        private static readonly string componentName = nameof(DataAssetProvider);

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAssetProvider" /> class.
        /// </summary>
        /// <param name="dataDiscoveryClientFactory">The data discovery client factory instance.</param>
        /// <param name="datagridConfiguration">The data grid configuration instance.</param>
        /// <param name="identityManifest">The identify manifest instance.</param>
        /// <param name="sessionFactory">The session factory instance.</param>
        /// <param name="eventWriterFactory"></param>
        public DataAssetProvider(
            IDataDiscoveryClientFactory dataDiscoveryClientFactory, 
            IDataGridConfiguration datagridConfiguration,
            IManifest identityManifest,
            ISessionFactory sessionFactory,
            IEventWriterFactory eventWriterFactory)
        {
            this.dataDiscoveryClientFactory = dataDiscoveryClientFactory;
            this.eventWriterFactory = eventWriterFactory;
            this.identityManifest = identityManifest;
            this.sessionFactory = sessionFactory;

            this.datagridConfiguration = datagridConfiguration;
            this.defaultDataAssetPageSize = datagridConfiguration.DefaultPageSize;
            this.maxDataAssetPageSize = datagridConfiguration.MaxPageSize;
        }

        public async Task<FilterResult<DataAsset>> FindDataAssetsByQualifierAsync(DataAssetFilterCriteria filterCriteria, AssetQualifier qualifier, bool includeTags)
        {
            filterCriteria.Count = filterCriteria.Count ?? this.defaultDataAssetPageSize;
            filterCriteria.Initialize(this.maxDataAssetPageSize);

            DataGridSearch dataGridSearch = DataGridSearch.CreateFromAssetQualifier(
                qualifier, 
                this.datagridConfiguration.UseTransitionPropertiesAssetTypes,
                this.datagridConfiguration.UseSearchPropertiesAssetTypes);

            if (!(dataGridSearch.FiltersForSearchRequest is Dictionary<string, List<string>> searchFilters))
            {
                throw new ArgumentException($"Null DataGridSearch filters: {qualifier.GetValueForSearch(this.datagridConfiguration.UseTransitionPropertiesAssetTypes)}.");
            }

            var request = new SearchRequest
            {
                Filters = searchFilters,
                PageNumber = filterCriteria.Index.Value + 1, // DataGrid uses 1 based index instead of 0 based.
                PageSize = filterCriteria.Count.Value,
                IncludeFields = new List<string> { "Taxonomy", "PrimaryIdentifier" }
            };

            if (includeTags)
            {
                request.IncludeFields.Add("PrivacyDataType");
            }

            var assetType = this.GetAssetType(qualifier);
            var dataDiscoveryClient = await this.dataDiscoveryClientFactory.CreateClientAsync().ConfigureAwait(false);

            Tuple<SearchResponse, string, string> response;
            try
            {
                // Return all assets from DataGrid except for Internal assets
                response = await this.sessionFactory.InstrumentAsync(
                    "DataGrid.SearchAssetsAsync",
                    SessionType.Outgoing,
                    async () =>
                    {
                        var value = await dataDiscoveryClient.SearchAssetsAsync(request, assetType, AssetVisibility.Public | AssetVisibility.Private | AssetVisibility.Sensitive).ConfigureAwait(false);
                        string requestTerms = "{" + string.Join(",", request.Filters.Select(kv => kv.Key + "=" + kv.Value?.First().ToString()).ToArray()) + "}";

                        return new Tuple<SearchResponse, string, string>(value, assetType.ToString(), requestTerms);
                    }).ConfigureAwait(false);

                // The DataGrid query can return false positives, so we need to perform additional filtering.
                var values = this.ParseResponse(qualifier.AssetType, response.Item1, this.datagridConfiguration.UseTransitionPropertiesAssetTypes);
                var filteredValues = values.Where(x => qualifier.Contains(x.Qualifier, this.datagridConfiguration.UseMatchPropertiesAssetTypes));

                return new FilterResult<DataAsset>
                {
                    Values = filteredValues,
                    Index = filterCriteria.Index.Value,
                    Count = filterCriteria.Count.Value,
                    Total = response.Item1.TotalHits
                };
            }
            catch (Exception ex)
            {
                this.eventWriterFactory.Trace(nameof(DataAssetProvider), $"Error calling DataGrid. Exception is {ex.ToString()}");
                throw;
            }
        }

        private IEnumerable<DataAsset> ParseResponse(Identity.AssetType assetType, SearchResponse response, string useTransitionPropertiesAssetTypes)
        {
            List<DataAsset> results = new List<DataAsset>();

            foreach (var result in response.SearchResults)
            {
                try
                {
                    // Perform explicit conversion to JObject (implicit conversion was returning null)
                    JObject taxonomy = JObject.FromObject(result.Taxonomy);
                    DataAsset asset = new DataAsset
                    {
                        Id = result.PrimaryIdentifier,
                        Qualifier = AssetQualifier.CreateFromDataGridTaxonomy(assetType, taxonomy, useTransitionPropertiesAssetTypes),
                        Tags = this.ParseTags(result)
                    };

                    results.Add(asset);
                }
                catch (Exception ex)
                {
                    // If datagrid returns qualifiers that are not valid, we need to skip them.
                    this.eventWriterFactory.SuppressedException(
                        componentName,
                        new SuppressedException("DataGrid.ParseResponse", ex));
                }
            }

            return results;
        }

        private AssetTags ParseTags(Asset asset)
        {
            if (asset.PrivacyDataType == null)
            {
                return null;
            }
            else
            {
                var allTags = new List<Tag>();

                if (asset.PrivacyDataType.AssetTags != null)
                {
                    foreach (var assetTag in asset.PrivacyDataType.AssetTags)
                    {
                        allTags.Add(new Tag { Name = assetTag.TagName });
                    }
                }

                if (asset.PrivacyDataType.AttributeTags != null)
                {
                    foreach (var attributeTag in asset.PrivacyDataType.AttributeTags)
                    {
                        allTags.Add(new Tag { Name = attributeTag.TagName });
                    }
                }

                var subjectTags = new List<Tag>();
                var dataTags = new List<Tag>();
                bool isNonPersonal = false;
                bool isLongTail = false;

                foreach (var tag in allTags)
                {
                    if (tag.Name.StartsWith("Privacy.Subject") || tag.Name.StartsWith("Privacy.Asset.SubjectType"))
                    {
                        subjectTags.Add(tag);
                    }
                    else if (tag.Name.StartsWith("Privacy.DataType"))
                    {
                        dataTags.Add(tag);
                    }
                    else if (tag.Name.Equals("Privacy.Asset.NonPersonal"))
                    {
                        isNonPersonal = true;
                    }
                    else if (tag.Name.StartsWith("Privacy.Asset.LongTail"))
                    {
                        isLongTail = true;
                    }
                    else if (tag.Name.StartsWith("Privacy.Asset.CustomNonUse"))
                    {
                        isLongTail = true;
                    }
                }

                return new AssetTags
                {
                    SubjectTypes = subjectTags,
                    DataTypes = dataTags,
                    IsNonPersonal = isNonPersonal,
                    IsLongTailOrCustomNonUse = isLongTail
                };
            }
        }

        private AssetType GetAssetType(AssetQualifier qualifier)
        {
            var typeDefinition = this.GetMetadata(qualifier.AssetType);

            AssetType assetType;

            if (Enum.TryParse(typeDefinition.DataGridId, out assetType))
            {
                return assetType;
            }
            else
            {
                throw new InvalidOperationException($"Unknown asset type: {qualifier.AssetType}.");
            }
        }

        private AssetTypeDefinition GetMetadata(Identity.AssetType assetType)
        {
            return this.identityManifest.AssetTypes.Single(x => x.Id == assetType);
        }
    }
}