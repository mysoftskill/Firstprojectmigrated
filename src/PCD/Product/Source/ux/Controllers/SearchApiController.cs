using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.PrivacyServices.Identity;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.Search;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    public class SearchApiController : Controller
    {
        private readonly IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor;

        public SearchApiController(
            IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor)
        {
            this.pdmsClientProviderAccessor = pdmsClientProviderAccessor ?? throw new ArgumentNullException(nameof(pdmsClientProviderAccessor));
        }

        [HttpGet]
        [ResponseCache(Duration = 15 * 60 /* seconds. */)]
        public Task<IActionResult> Search(string terms)
        {
            if (Guid.TryParse(terms, out _))
            {
                return SearchById(terms);
            }

            if (IsAssetGroupQualifier(terms))
            {
                return SearchByAssetGroupQualifier(terms);
            }

            if (TryParseSearchTermsAsCosmosUri(terms, out var cosmosAssetGroupQualifier))
            {
                return SearchByAssetGroupQualifier(cosmosAssetGroupQualifier);
            }

            return SearchByTerms(terms);

            bool IsAssetGroupQualifier(string assetGroupQualifierCandidate)
            {
                try
                {
                    AssetQualifier.Parse(assetGroupQualifierCandidate);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            bool TryParseSearchTermsAsCosmosUri(string searchTerms, out string assetGroupQualifier)
            {
                assetGroupQualifier = null;

                try
                {
                    //  Attempt to convert an absolute URI like this to an asset group qualifier.
                    //  https://cosmos15.osdinfra.net/cosmos/msn/local/Experimentation/SFDataCooking/Cooked/DailySFWCookedData/2018/02/SFW_Cooked_2018-02-20.ss
                    var cosmosAssetGroupQualifierCandidate = AssetQualifier.CreateForCosmosStreamSetUri(searchTerms);

                    //  Cosmos asset group qualifier has PhysicalCluster, VirtualCluster and RelativePath parts.
                    //  Since input string most likely points to a file in a streamset, and users are registering
                    //  streamset definitions instead of singular files, we'll erase RelativePath part,
                    //  so the search would look at a higher level of the asset, thus increasing our chances to
                    //  find actual registration.
                    //  Potential improvements: produce several variations of the RelativePath, increasing search
                    //  radius on each variation.
                    cosmosAssetGroupQualifierCandidate.Properties.Remove("RelativePath");

                    assetGroupQualifier = AssetQualifier.CreateFromDictionary(cosmosAssetGroupQualifierCandidate.Properties).ToString();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Searches PDMS entities using ID as a key.
        /// </summary>
        /// <param name="id">ID to search for.</param>
        private async Task<IActionResult> SearchById(string id)
        {
            var pdmsClientInstance = pdmsClientProviderAccessor.ProviderInstance.Instance;

            // find Service Tree Ids
            var servicetreefilter = new PdmsApiModelsV2.ServiceTreeFilterCriteria { ServiceId = StringContains(id) };
            var dataOwnerFilter = new PdmsApiModelsV2.DataOwnerFilterCriteria { ServiceTree = servicetreefilter };
            var findOwnersByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataOwners.ReadByFiltersAsync(ctx, filterCriteria: dataOwnerFilter)).Response.Value);
            if (findOwnersByFilterTask.Result != null && findOwnersByFilterTask.Result.Any())
            {
                id = findOwnersByFilterTask.Result.FirstOrDefault().Id;
            }

            //  Find data owners.
            var findOwnersTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataOwners.ReadAsync(id, ctx)).Response);

            //  Find data agents.
            var findDataAgentsTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataAgents.ReadAsync<PdmsApiModelsV2.DeleteAgent>(id, ctx)).Response);
            var dataAgentFilter = new PdmsApiModelsV2.DataAgentFilterCriteria { OwnerId = id };
            var findDataAgentsByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataAgents.ReadByFiltersAsync(ctx, filterCriteria: dataAgentFilter)).Response.Value);

            //  Find data assets.
            var findAssetGroupsTask = SafeRead(
                async ctx => (await pdmsClientInstance.AssetGroups.ReadAsync(id, ctx)).Response);
            var dataAssetsFilter = new PdmsApiModelsV2.AssetGroupFilterCriteria { OwnerId = id };
            var findDataAssetsByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.AssetGroups.ReadAllByFiltersAsync(ctx, filterCriteria: dataAssetsFilter)).Response);

            //  Find variants.
            var findVariantsTask = SafeRead(
                async ctx => (await pdmsClientInstance.VariantDefinitions.ReadAsync(id, ctx)).Response);
            var variantsFilter = new PdmsApiModelsV2.VariantDefinitionFilterCriteria { OwnerId = id };
            var findVariantsByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.VariantDefinitions.ReadAllByFiltersAsync(ctx, filterCriteria: variantsFilter)).Response);

            //  Find variant requests.
            var findVariantRequestsTask = SafeRead(
                async ctx => (await pdmsClientInstance.VariantRequests.ReadAsync(id, ctx)).Response);
            var variantRequestsFilter = new PdmsApiModelsV2.VariantRequestFilterCriteria { OwnerId = id };
            var findVariantRequestsByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.VariantRequests.ReadAllByFiltersAsync(ctx, filterCriteria: variantRequestsFilter)).Response);

            //  Find sharing requests.
            var findSharingRequestsTask = SafeRead(
                async ctx => (await pdmsClientInstance.SharingRequests.ReadAsync(id, ctx)).Response);
            var sharingRequestsFilter = new PdmsApiModelsV2.SharingRequestFilterCriteria
            {
                OwnerId = id,
                Or = new PdmsApiModelsV2.SharingRequestFilterCriteria
                {
                    DeleteAgentId = id
                }
            };
            var findSharingRequestsByFilterTask = SafeRead(
                async ctx => (await pdmsClientInstance.SharingRequests.ReadAllByFiltersAsync(ctx, filterCriteria: sharingRequestsFilter)).Response);

            try
            {
                await Task.WhenAll(findOwnersTask,
                                   findDataAgentsTask, 
                                   findDataAgentsByFilterTask,
                                   findAssetGroupsTask,
                                   findVariantsTask,
                                   findVariantRequestsTask, 
                                   findVariantRequestsByFilterTask,
                                   findSharingRequestsTask,
                                   findSharingRequestsByFilterTask);
            }
            catch
            {
                //  Do nothing.
            }
                
            return new JsonResult(new
            {
                owners = SearchResultsOf(findOwnersTask, DataOwnerAsSearchResult),
                dataAgents = Concat(SearchResultsOf(findDataAgentsTask, DataAgentAsSearchResult),
                                    SearchResultsOf(findDataAgentsByFilterTask, e => ListOf(e, DataAgentAsSearchResult))),
                variants = SearchResultsOf(findVariantsTask, VariantAsSearchResult),
                variantRequests = Concat(SearchResultsOf(findVariantRequestsByFilterTask, e => ListOf(e, VariantRequestAsSearchResult)),
                                         SearchResultsOf(findVariantRequestsTask, VariantRequestAsSearchResult)),
                sharingRequests = Concat(SearchResultsOf(findSharingRequestsByFilterTask, e => ListOf(e, SharingRequestAsSearchResult)),
                                         SearchResultsOf(findSharingRequestsTask, SharingRequestAsSearchResult)),
                assetGroups = Concat(SearchResultsOf(findAssetGroupsTask, AssetGroupAsSearchResult),
                                         SearchResultsOf(findDataAssetsByFilterTask, e => ListOf(e, AssetGroupAsSearchResult))),
            });
        }

        /// <summary>
        /// Searches PDMS entities by string terms.
        /// </summary>
        /// <param name="terms">Terms to search for.</param>
        private async Task<IActionResult> SearchByTerms(string terms)
        {
            var pdmsClientInstance = pdmsClientProviderAccessor.ProviderInstance.Instance;

            var ownerFilter = new PdmsApiModelsV2.DataOwnerFilterCriteria { Name = StringContains(terms) };
            var findOwnersTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataOwners.ReadByFiltersAsync(ctx, filterCriteria: ownerFilter)).Response.Value);

            var dataAgentFilter = new PdmsApiModelsV2.DataAgentFilterCriteria { Name = StringContains(terms) };
            var findDataAgentsTask = SafeRead(
                async ctx => (await pdmsClientInstance.DataAgents.ReadByFiltersAsync(ctx, filterCriteria: dataAgentFilter)).Response.Value);

            var variantFilter = new PdmsApiModelsV2.VariantDefinitionFilterCriteria { Name = StringContains(terms) };
            var findVariantsTask = SafeRead(
                async ctx => (await pdmsClientInstance.VariantDefinitions.ReadAllByFiltersAsync(ctx, filterCriteria: variantFilter)).Response);

            try
            {
                await Task.WhenAll(findOwnersTask, findDataAgentsTask, findVariantsTask);
            
                return new JsonResult(new
                {
                    owners = SearchResultsOf(findOwnersTask, e => ListOf(e, DataOwnerAsSearchResult)),
                    dataAgents = SearchResultsOf(findDataAgentsTask, e => ListOf(e, DataAgentAsSearchResult)),
                    variants = SearchResultsOf(findVariantsTask, e => ListOf(e, VariantAsSearchResult))
                });
            }
            catch
            {
                return new JsonResult(new { });
            }
        }

        /// <summary>
        /// Searches asset groups by asset group qualifier.
        /// </summary>
        /// <param name="assetGroupQualifier">AssetGroup qualifier to search for.</param>
        private async Task<IActionResult> SearchByAssetGroupQualifier(string assetGroupQualifier)
        {
            var pdmsClientInstance = pdmsClientProviderAccessor.ProviderInstance.Instance;

            var assetGroupFilter = new PdmsApiModelsV2.AssetGroupFilterCriteria { Qualifier = StringContains(assetGroupQualifier) };
            var findAssetGroupsTask = SafeRead(
                async ctx => (await pdmsClientInstance.AssetGroups.ReadAllByFiltersAsync(ctx, filterCriteria: assetGroupFilter)).Response);

            try
            {
                await Task.WhenAll(findAssetGroupsTask);
            }
            catch
            {
                //  Do nothing.
            }

            return new JsonResult(new
            {
                assetGroups = SearchResultsOf(findAssetGroupsTask, e => ListOf(e, AssetGroupAsSearchResult))
            });
        }

        /// <summary>
        /// A shortcut to produce "string contains" filter clause.
        /// </summary>
        /// <param name="value">Value to filter by.</param>
        private static DataManagement.Client.Filters.StringFilter StringContains(string value) => 
            new DataManagement.Client.Filters.StringFilter(value, DataManagement.Client.Filters.StringComparisonType.Contains);

        /// <summary>
        /// Reads PDMS result, while handling <see cref="PdmsApiModelsV2.NotFoundError.Entity"/> exception.
        /// If exception occurred, null will be returned.
        /// </summary>
        private Task<TResult> SafeRead<TResult>(Func<DataManagement.Client.RequestContext, Task<TResult>> readFunc) =>
            SafeRead<TResult, PdmsApiModelsV2.NotFoundError.Entity>(readFunc);

        /// <summary>
        /// Reads PDMS result, while handling exception of a known type.
        /// If exception occurred, null will be returned.
        /// </summary>
        private async Task<TResult> SafeRead<TResult, TException>(Func<DataManagement.Client.RequestContext, Task<TResult>> readFunc)
            where TException : Exception
        {
            try
            {
                return await readFunc(await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
            }
            catch (TException)
            {
                return default(TResult);
            }
        }

        /// <summary>
        /// Converts task with search results into actual search results.
        /// </summary>
        /// <typeparam name="TEntity">Type of PDMS entity.</typeparam>
        /// <param name="taskWithResult">Task with search results.</param>
        /// <param name="convertToSearchResult">PDMS entity to search results converter.</param>
        private SearchResultBase SearchResultsOf<TEntity>(Task<TEntity> taskWithResult, Func<TEntity, SearchResultBase> convertToSearchResult)
        {
            if (!taskWithResult.IsCompleted)
            {
                return new SearchErrorResult();
            }

            if (null == taskWithResult.Result)
            {
                return null;
            }

            var result = convertToSearchResult(taskWithResult.Result);
            return (null == result || result is SearchEntitiesResult) ? result : new SearchEntitiesResult { Entities = new[] { result } };
        }

        /// <summary>
        /// Converts list of found PDMS entities to <see cref="SearchEntitiesResult"/>.
        /// </summary>
        /// <typeparam name="TEntity">Type of PDMS entity.</typeparam>
        /// <param name="entities">List of PDMS entities.</param>
        /// <param name="convertToSearchResult">PDMS entity to search results converter.</param>
        private static SearchEntitiesResult ListOf<TEntity>(IEnumerable<TEntity> entities, Func<TEntity, SearchResultBase> convertToSearchResult) =>
            entities.Any() ? new SearchEntitiesResult { Entities = entities.Select(convertToSearchResult) } : null;

        /// <summary>
        /// Concatenates two search results into one.
        /// </summary>
        /// <param name="searchResult1">First search result to concatenate.</param>
        /// <param name="searchResult2">Second search result to concatenate.</param>
        private static SearchResultBase Concat(SearchResultBase searchResult1, SearchResultBase searchResult2)
        {
            if (null == searchResult1 || null == searchResult2)
            {
                return searchResult1 ?? searchResult2;
            }

            if (searchResult1 is SearchErrorResult)
            {
                return searchResult2;
            }
            if (searchResult2 is SearchErrorResult)
            {
                return searchResult1;
            }

            return new SearchEntitiesResult
            {
                Entities = Enumerable.Concat(Expand(searchResult1), Expand(searchResult2))
            };

            IEnumerable<SearchResultBase> Expand(SearchResultBase searchResult)
            {
                if (searchResult is SearchEntitiesResult entitiesResult)
                {
                    return entitiesResult.Entities;
                }
                else
                {
                    return new[] { searchResult };
                }
            }
        }

        #region Entity Converters

        private static SearchEntityResult DataOwnerAsSearchResult(PdmsApiModelsV2.DataOwner dataOwner) => new SearchEntityResult
        {
            Id = dataOwner.Id,
            Name = dataOwner.Name,
            Description = dataOwner.Description
        };

        private static SearchEntityResult DataAgentAsSearchResult(PdmsApiModelsV2.DataAgent dataAgent) => new SearchEntityResult
        {
            Id = dataAgent.Id,
            Name = dataAgent.Name,
            Description = dataAgent.Description,
            OwnerId = dataAgent.OwnerId
        };

        private static SearchResultBase AssetGroupAsSearchResult(PdmsApiModelsV2.AssetGroup assetGroup) => new SearchAssetGroupResult
        {
            Id = assetGroup.Id,
            OwnerId = assetGroup.OwnerId,
            DeleteAgentId = assetGroup.DeleteAgentId,
            ExportAgentId = assetGroup.ExportAgentId,
            Qualifier = new Models.Pdms.AssetGroupQualifier(assetGroup.Qualifier.Properties)
        };

        private static SearchEntityResult VariantAsSearchResult(PdmsApiModelsV2.VariantDefinition variant) => new SearchEntityResult
        {
            Id = variant.Id,
            Name = variant.Name,
            Description = variant.Description,
            OwnerId = variant.OwnerId
        };

        private static SearchEntityResult VariantRequestAsSearchResult(PdmsApiModelsV2.VariantRequest variantRequest) => new SearchEntityResult
        {
            Id = variantRequest.Id,
            OwnerId = variantRequest.OwnerId,
            OwnerName = variantRequest.OwnerName,
        };

        private static SearchEntityResult SharingRequestAsSearchResult(PdmsApiModelsV2.SharingRequest sharingRequest) => new SearchEntityResult
        {
            Id = sharingRequest.Id,
            OwnerId = sharingRequest.OwnerId,
            OwnerName = sharingRequest.OwnerName,
            AgentId = sharingRequest.DeleteAgentId,
        };

        #endregion
    }
}
