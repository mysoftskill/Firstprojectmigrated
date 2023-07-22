using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.V2;
using Microsoft.PrivacyServices.Identity;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class DataAssetMockCooker
    {
        private readonly IFixture fixture;
        private readonly CookerUtility cookerUtility;

        public DataAssetMockCooker(IFixture iFixture)
        {
            fixture = iFixture;
            cookerUtility = new CookerUtility(fixture);
        }

        public Func<Task<IHttpResult<IEnumerable<AssetGroup>>>> CookListOfAssetGroupsFor(string teamName)
        {
            return () => {
                return CookHttpResultEnumerableTaskFor(cookerUtility.CookListFrom(new[] {
                    CookRawAssetGroupFor("Asset1", teamName, AssetType.CosmosStructuredStream),
                    CookRawAssetGroupFor("Asset2", teamName, AssetType.CosmosStructuredStream)
                }));
            };
        }

        public Func<Task<IHttpResult<AssetGroup>>> CookAssetGroupFor(string teamName)
        {
            return () => {
                return CookHttpResultTaskFor(CookRawAssetGroupFor("Asset1", teamName, AssetType.CosmosStructuredStream));
            };
        }

        public Func<Task<IHttpResult>> CookEmptyHttpResult()
        {
            return cookerUtility.CookEmptyHttpResult();
        }

        public Func<Task<IHttpResult<Collection<AssetGroup>>>> CookListOfAssetGroupsWithCountFor(string teamName)
        {
            return () => {
                return CookHttpResultCollectionTaskFor(fixture.Build<Collection<AssetGroup>>()
                                                                .With(m => m.Total, cookerUtility.GetNumberFromName(teamName))
                                                                .Create());
            };
        }

        private AssetGroup CookRawAssetGroupFor(string assetName, string teamName, AssetType assetType)
        {
            IDictionary<string, string> properties;
            IEnumerable<AssetGroupVariant> variants = cookerUtility.CookListFrom(new[] {
                fixture.Build<AssetGroupVariant>()
                        .With(m2 => m2.VariantId, cookerUtility.GenerateFuzzyGuidFromName(assetName).ToString())
                        .With(m2 => m2.VariantState, VariantState.Approved)
                        .Create()
            });

            switch (assetType)
            { 
                case AssetType.CosmosStructuredStream:
                    properties = fixture.Build<Dictionary<string, string>>()
                                            .Do(m => { 
                                                m["AssetType"] = AssetType.CosmosStructuredStream.ToString();
                                                m["PhysicalCluster"] = $"I9n_{assetName}_{teamName}_PhysicalCluster";
                                                m["VirtualCluster"] = $"I9n_{assetName}_{teamName}_VirtualCluster";
                                                m["RelativePath"] = $"/local/I9n_{assetName}_{teamName}_RelativePath";
                                            })
                                            .Create();
                    break;
                default:
                    properties = fixture.Build<Dictionary<string, string>>()
                                            .Do(m => {
                                                m["AssetType"] = AssetType.CosmosStructuredStream.ToString();
                                                m["PhysicalCluster"] = $"I9n_{assetName}_{teamName}_PhysicalCluster";
                                                m["VirtualCluster"] = $"I9n_{assetName}_{teamName}_VirtualCluster";
                                                m["RelativePath"] = $"/local/I9n_{assetName}_{teamName}_RelativePath";
                                            })
                                            .Create();
                    break;
            }

            return fixture.Build<AssetGroup>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(assetName).ToString())
                            .With(m => m.OwnerId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                            .With(m => m.Qualifier, AssetQualifier.CreateFromDictionary(properties))
                            .With(m => m.Variants, variants)
                            .Create();
        }

        private Task<IHttpResult<Collection<AssetGroup>>> CookHttpResultCollectionTaskFor(Collection<AssetGroup> assetGroups)
        {
            IHttpResult<Collection<AssetGroup>> result = new HttpResult<Collection<AssetGroup>>(
                cookerUtility.CookHttpResultFor("ReadByFiltersAsync"), assetGroups);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<IEnumerable<AssetGroup>>> CookHttpResultEnumerableTaskFor(IEnumerable<AssetGroup> assetGroups)
        {
            IHttpResult<IEnumerable<AssetGroup>> result = new HttpResult<IEnumerable<AssetGroup>>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), assetGroups);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<AssetGroup>> CookHttpResultTaskFor(AssetGroup assetGroup)
        {
            IHttpResult<AssetGroup> result = new HttpResult<AssetGroup>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), assetGroup);
            return Task.FromResult(result);
        }
    }
}
