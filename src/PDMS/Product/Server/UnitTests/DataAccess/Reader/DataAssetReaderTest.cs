namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using DataGrid = DataPlatform.DataDiscoveryService.Contracts;

    using Microsoft.DataPlatform.DataDiscovery;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataGridService;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Newtonsoft.Json.Linq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataAssetReaderTest
    {
        [Theory(DisplayName = "When FindByQualifier is called, then the FilterResult is set correctly."), ValidData]
        public async Task When_FindByQualifierIsCalled_Then_FilterResultIsSetCorrectly(
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            AssetQualifier qualifier,
            DataAssetFilterCriteria filterCriteria,
            [Frozen] DataGrid.SearchResponse response)
        {
            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Assert.Equal(filterCriteria.Index.Value, result.Index); // Our code is 0 indexed, so the result should not be +1.
            Assert.Equal(filterCriteria.Count.Value, result.Count);
            Assert.Equal(response.TotalHits, result.Total);
        }

        [Theory(DisplayName = "Verify the correct paging values are passed to DataGrid."), ValidData]
        public async Task VerifyPagingValues(
            [Frozen] Mock<IDataDiscoveryClient> dataGrid,
            AssetQualifier qualifier,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria)
        {
            filterCriteria.Index = 2;
            filterCriteria.Count = 5;

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Action<DataGrid.SearchRequest> verifyRequest = x =>
            {
                Assert.Equal(3, x.PageNumber); // DataGrid is 1 index instead of 0 index. So this should be 1 more than the input value.
                Assert.Equal(5, x.PageSize);
            };

            dataGrid.Verify(x => x.SearchAssetsAsync(Is.Value(verifyRequest), It.IsAny<DataGrid.AssetType>(), DataGrid.AssetVisibility.Public | DataGrid.AssetVisibility.Private | DataGrid.AssetVisibility.Sensitive), Times.Once);
        }

        [Theory(DisplayName = "Verify the default paging values are passed to DataGrid if none are provided."), ValidData]
        public async Task VerifyDefaultPagingValues(
            [Frozen] Mock<IDataDiscoveryClient> dataGrid,
            AssetQualifier qualifier,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria)
        {
            filterCriteria.Index = null;
            filterCriteria.Count = null;

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Action<DataGrid.SearchRequest> verifyRequest = x =>
            {
                Assert.Equal(1, x.PageNumber); // DataGrid is 1 index instead of 0 index. So this should be 1 more than the input value.
                Assert.Equal(10, x.PageSize); // This is the hard-coded config value from the ValidData attribute.
            };

            dataGrid.Verify(x => x.SearchAssetsAsync(Is.Value(verifyRequest), It.IsAny<DataGrid.AssetType>(), It.IsAny<DataGrid.AssetVisibility>()), Times.Once);
        }

        [Theory(DisplayName = "Verify the max paging values are passed to DataGrid if the input is too large."), ValidData]
        public async Task VerifyMaxPagingValues(
            [Frozen] Mock<IDataDiscoveryClient> dataGrid,
            AssetQualifier qualifier,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria)
        {
            filterCriteria.Index = -1;
            filterCriteria.Count = 50;

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Action<DataGrid.SearchRequest> verifyRequest = x =>
            {
                Assert.Equal(1, x.PageNumber); // DataGrid is 1 index instead of 0 index. So this should be 1 more than the input value.
                Assert.Equal(20, x.PageSize); // This is the hard-coded config value from the ValidData attribute.
            };

            dataGrid.Verify(x => x.SearchAssetsAsync(Is.Value(verifyRequest), It.IsAny<DataGrid.AssetType>(), DataGrid.AssetVisibility.Public | DataGrid.AssetVisibility.Private | DataGrid.AssetVisibility.Sensitive), Times.Once);
        }

        [Theory(DisplayName = "Verify the asset qualifier is parsed correctly.")]
        [CombinedInlineData("AssetType=API;Host=https://p1.com;Path=/p2;Method=PUT", "Taxonomy.Host.ngram:https://p1.com && Taxonomy.Path.ngram:/p2 && Taxonomy.Method.ngram:put", DataGrid.AssetType.API)]
        [CombinedInlineData("AssetType=ApacheCassandra;Host=P1;Keyspace=P2;TableName=P3", "Taxonomy.HostNormalized.ngram:p1 && Taxonomy.KeyspaceNormalized.ngram:p2 && Taxonomy.TableNormalized.ngram:p3", DataGrid.AssetType.ApacheCassandra)]
        [CombinedInlineData("AssetType=ApacheHadoop;Cluster=P1;Path=P2", "Taxonomy.ClusterNormalized.ngram:p1 && Taxonomy.PathNormalized.ngram:P2", DataGrid.AssetType.ApacheHadoop)]
        [CombinedInlineData("AssetType=ApacheHive;Host=P1;DatabaseName=P2;TableName=P3", "Taxonomy.HostNormalized.ngram:p1.azurehdinsight.net && Taxonomy.DatabaseNormalized.ngram:p2 && Taxonomy.TableNormalized.ngram:p3", DataGrid.AssetType.ApacheHive)]
        [CombinedInlineData("AssetType=Avocado;Host=P1;Path=P2", "Taxonomy.Host.ngram:p1 && Taxonomy.Path.ngram:p2", DataGrid.AssetType.Avocado)]
        [CombinedInlineData("AssetType=AzureBlob;AccountName=P1;ContainerName=P2", "Taxonomy.AccountNormalized.ngram:p1.blob.core.windows.net && Taxonomy.ContainerNormalized.ngram:p2", DataGrid.AssetType.AzureBlob)]
        [CombinedInlineData("AssetType=AzureDataLake;AccountName=P1;Path=P2", "Taxonomy.AccountNameNormalized.ngram:p1.azuredatalakestore.net && Taxonomy.PathNormalized:P2/", DataGrid.AssetType.AzureDataLake)]
        [CombinedInlineData("AssetType=AzureDocumentDB;AccountName=p1.documents.azure.com", "Taxonomy.AccountName.ngram:p1", DataGrid.AssetType.AzureCosmosDb)]
        [CombinedInlineData("AssetType=AzureEventHub;Endpoint=p1;EventHub=p2;ConsumerGroup=P3", "Taxonomy.Endpoint:p1 && Taxonomy.EventHub.ngram:p2 && Taxonomy.ConsumerGroup.ngram:p3", DataGrid.AssetType.AzureEventHub)]
        [CombinedInlineData("AssetType=AzureHDInsight;Host=p1;Path=p2", "Taxonomy.AccountNormalized.ngram:p1.azurehdinsight.net && Taxonomy.PathNormalized:p2", DataGrid.AssetType.AzureHDInsight)]
        [CombinedInlineData("AssetType=AzureRedisCache;AccountName=p1;DatabaseName=P2", "Taxonomy.AccountNormalized.ngram:p1.redis.cache.windows.net && Taxonomy.DatabaseNormalized.ngram:p2", DataGrid.AssetType.AzureRedisCache)]
        [CombinedInlineData("AssetType=AzureSearch;Host=p1;Index=p2", "Taxonomy.ServiceNormalized.ngram:p1.search.windows.net && Taxonomy.IndexNormalized.ngram:p2", DataGrid.AssetType.AzureSearch)]
        [CombinedInlineData("AssetType=AzureServiceBusSubscription;Endpoint=p1;Subscription=p2;Topic=P3", "Taxonomy.NamespaceNormalized.ngram:p1.servicebus.windows.net && Taxonomy.SubscriptionNormalized.ngram:p2 && Taxonomy.TopicNormalized.ngram:p3", DataGrid.AssetType.AzureServiceBusSubscription)]
        [CombinedInlineData("AssetType=AzureSql;ServerName=p1", "Taxonomy.ServerNameNormalized.ngram:p1", DataGrid.AssetType.AzureSql)]
        [CombinedInlineData("AssetType=AzureSqlDW;ServerName=p1;DatabaseName=P2;TableName=P3", "Taxonomy.ServerNameNormalized.ngram:p1.database.windows.net && Taxonomy.DatabaseNameNormalized.ngram:p2 && Taxonomy.TableNameNormalized.ngram:[dbo].[p3]", DataGrid.AssetType.AzureSqlDW)]
        [CombinedInlineData("AssetType=ClouderaImpala;Host=P1;Path=P2", "Taxonomy.Host.ngram:p1 && Taxonomy.Path.ngram:p2", DataGrid.AssetType.ClouderaImpala)]
        [CombinedInlineData("AssetType=CosmosStructuredStream;PhysicalCluster=p1;VirtualCluster=p2;RelativePath=/local/Test", "Taxonomy.PhysicalCluster.ngram:p1 && Taxonomy.VirtualCluster.ngram:p2 && Taxonomy.RelativePath.ngram:/local/Test/", DataGrid.AssetType.CosmosStructuredStream)]
        [CombinedInlineData("AssetType=CosmosUnstructuredStream;PhysicalCluster=p1;VirtualCluster=p2", "Taxonomy.PhysicalCluster.ngram:p1 && Taxonomy.VirtualCluster.ngram:p2", DataGrid.AssetType.CosmosUnstructuredStream)]
        [CombinedInlineData("AssetType=File;ServerPath=p1", "Taxonomy.ServerPathNormalized.ngram:\\\\p1", DataGrid.AssetType.File)]
        [CombinedInlineData("AssetType=Kusto;ClusterName=p1", "Taxonomy.ClusterName.ngram:p1", DataGrid.AssetType.Kusto)]
        [CombinedInlineData("AssetType=MongoDB;Host=P1;DatabaseName=P2", "Taxonomy.HostNormalized.ngram:p1 && Taxonomy.DatabaseNameNormalized.ngram:p2", DataGrid.AssetType.MongoDB)]
        [CombinedInlineData("AssetType=MySQL;ServerName=p1;DatabaseName=P2;TableName=P3", "Taxonomy.ServerNormalized.ngram:p1.mysql.database.azure.com && Taxonomy.DatabaseNormalized.ngram:p2 && Taxonomy.TableNormalized.ngram:p3", DataGrid.AssetType.MySQL)]
        [CombinedInlineData("AssetType=ObjectStore;Environment=P1;Namespace=P2;TableName=P3", "Taxonomy.Environment.ngram:p1 && Taxonomy.Namespace.ngram:p2 && Taxonomy.TableName.ngram:p3", DataGrid.AssetType.ObjectStore)]
        [CombinedInlineData("AssetType=PostgreSQL;Host=P1;DatabaseName=P2;TableName=P3", "Taxonomy.HostNormalized.ngram:p1.postgres.database.azure.com && Taxonomy.DatabaseNormalized.ngram:p2 && Taxonomy.TableNormalized.ngram:public.p3", DataGrid.AssetType.PostgreSQL)]
        [CombinedInlineData("AssetType=RocksDB;Host=P1;Path=P2", "Taxonomy.Host.ngram:p1 && Taxonomy.Path.ngram:p2", DataGrid.AssetType.RocksDB)]
        [CombinedInlineData("AssetType=Splunk;Host=p1", "Taxonomy.Host.ngram:p1", DataGrid.AssetType.Splunk)]
        [CombinedInlineData("AssetType=SqlServer;ServerName=p1", "Taxonomy.ServerNameNormalized.ngram:p1", DataGrid.AssetType.Sql)]
        [CombinedInlineData("AssetType=SqlServerAnalysisServices;ServerName=p1;DatabaseName=p2;CubeName=p3", "Taxonomy.ServerNormalized.ngram:p1 && Taxonomy.DatabaseNormalized.ngram:p2 && Taxonomy.CubeNormalized.ngram:p3", DataGrid.AssetType.SqlServerAnalysisServices)]
        [CombinedInlineData("AssetType=SqlServerStretchDatabase;ServerName=P1;DatabaseName=P2;TableName=P3", "Taxonomy.ServerName.ngram:p1 && Taxonomy.DatabaseName.ngram:p2 && Taxonomy.TableName.ngram:p3", DataGrid.AssetType.SqlServerStretchDatabase)]
        [CombinedInlineData("AssetType=SubstrateSDS;Application=P1;Collection=P2", "Taxonomy.Application.ngram:p1 && Taxonomy.Collection.ngram:p2", DataGrid.AssetType.SubstrateSDS)]
        [CombinedInlineData("AssetType=SubstrateSIGS;Signal=P1", "Taxonomy.Signal:P1", DataGrid.AssetType.SubstrateSIGS)]
        public async Task VerifyAssetTypeConversion(
            string qualifierValue,
            string searchTerm,
            DataGrid.AssetType assetType,
            [Frozen] Mock<IDataDiscoveryClient> dataGrid,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria)
        {
            var qualifier = AssetQualifier.Parse(qualifierValue);

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Action<DataGrid.SearchRequest> verifyRequest = x =>
            {
                var parts = searchTerm.Split(new[] { " && " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var values = part.Split(':');
                    var property = values[0];
                    var term = string.Join(":", values.Skip(1));

                    Assert.Contains(x.Filters, y => y.Key == property && y.Value.Single() == term);
                }
            };

            Action<DataGrid.AssetType> verifyAssetType = x =>
            {
                Assert.Equal(assetType, x);
            };

            dataGrid.Verify(x => x.SearchAssetsAsync(Is.Value(verifyRequest), Is.Value(verifyAssetType), DataGrid.AssetVisibility.Public | DataGrid.AssetVisibility.Private | DataGrid.AssetVisibility.Sensitive), Times.Once);
        }

        [Theory(DisplayName = "Verify invalid asset qualifiers drop the entry from the results.")]
        [CombinedInlineData("{'PhsycicalCluster':['p1'],'VirtualCluster':['p2'],'RelativePath':['/my/stream.ss']}", "AssetType=CosmosStructuredStream;PhysicalCluster=p1;VirtualCluster=p2")]        
        public async Task When_AnInvalidAssetQualifierIsReturned_Then_DropTheEntry(
            string jsonTaxonomy,
            string qualifierValue,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria,
            [Frozen] DataGrid.SearchResponse response)
        {
            var qualifier = AssetQualifier.Parse(qualifierValue);
            response.SearchResults = new List<DataGrid.Asset> { new DataGrid.Asset { Taxonomy = JObject.Parse(jsonTaxonomy) } };

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            var actualCount = result.Values.Count();
            Assert.Equal(0, actualCount);
        }

        [Theory(DisplayName = "Verify the data asset is parsed correctly.")]
        [CombinedInlineData("{'AccountNormalized':['p1'],'ContainerNormalized':['p2'],'PathNormalized':['p3']}", "AssetType=AzureBlob;AccountName=p1;ContainerName=p2;BlobPattern=p3")]
        [CombinedInlineData("{'AccountName':['https://p1'],'DatabaseName':['p2'],'CollectionName':['p3']}", "AssetType=AzureDocumentDB;AccountName=p1.documents.azure.com;DatabaseName=p2;CollectionName=p3")]
        [CombinedInlineData("{'DatabaseNameNormalized':['p1'],'TableNameNormalized':['p2'],'ServerNameNormalized':['p3']}", "AssetType=AzureSql;ServerName=p3;DatabaseName=p1;TableName=p2")]
        [CombinedInlineData("{'PhysicalCluster':['p1'],'VirtualCluster':['p2'],'RelativePath':['/local/']}", "AssetType=CosmosStructuredStream;PhysicalCluster=p1;VirtualCluster=p2;RelativePath=/local")]
        [CombinedInlineData("{'PhysicalCluster':['p1'],'VirtualCluster':['p2'],'RelativePath':['/local/']}", "AssetType=CosmosUnstructuredStream;PhysicalCluster=p1;VirtualCluster=p2;RelativePath=/local")]
        [CombinedInlineData("{'ServerPathNormalized':['p1'],'FileNormalized':['p2']}", "AssetType=File;ServerPath=p1;FileName=p2")]
        [CombinedInlineData("{'DatabaseName':['p1'],'TableName':['p2'],'ClusterName':['p3']}", "AssetType=Kusto;ClusterName=p3;DatabaseName=p1;TableName=p2")]
        [CombinedInlineData("{'DatabaseNameNormalized':['p1'],'TableNameNormalized':['p2'],'ServerNameNormalized':['p3']}", "AssetType=SqlServer;ServerName=p3;DatabaseName=p1;TableName=p2")]
        [CombinedInlineData("{'Endpoint':['sb://p1'],'EventHub':['p2'],'ConsumerGroup':['']}", "AssetType=AzureEventHub;Endpoint=sb://p1;EventHub=p2")]
        [CombinedInlineData("{'Endpoint':['sb://p1'],'EventHub':['p2'],'ConsumerGroup':''}", "AssetType=AzureEventHub;Endpoint=sb://p1;EventHub=p2")]
        [CombinedInlineData("{'NamespaceNormalized':['sb://p1'],'SubscriptionNormalized':['p2'],'TopicNormalized':'p3'}", "AssetType=AzureServiceBusSubscription;Endpoint=sb://p1;Subscription=p2;Topic=p3")]
        [CombinedInlineData("{'Host':'https://p1.com','Path':'/p2','Method':'PUT'}", "AssetType=API;Host=https://p1.com;Path=/p2;Method=PUT")]
        public async Task VerifyDataAssetConversion(
            string jsonTaxonomy,
            string qualifierValue,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria,
            [Frozen] DataGrid.SearchResponse response)
        {
            var qualifier = AssetQualifier.Parse(qualifierValue);
            response.SearchResults = new List<DataGrid.Asset> { new DataGrid.Asset { Taxonomy = JObject.Parse(jsonTaxonomy) } };

            var reader = this.CreateDataAssetReader(provider, principle);
            var result = await reader.FindByQualifierAsync(filterCriteria, qualifier, false).ConfigureAwait(false);

            Assert.Equal(qualifier, result.Values.Single().Qualifier);
        }

        [Theory(DisplayName = "Verify MissingWritePermissionException if authoriztion is denied.")]
        [CombinedInlineData("Authorization has been denied for this request.")]
        [CombinedInlineData("User does not have {0} access to perform operation on type {1}.")]
        public async Task VerifyMappingAuthorizationException(
            string errorMessage,
            [Frozen] Mock<IDataDiscoveryClient> dataGrid,
            DataAssetProvider provider,
            AuthenticatedPrincipal principle,
            DataAssetFilterCriteria filterCriteria)
        {
            dataGrid
                .Setup(x => x.SearchAssetsAsync(It.IsAny<DataGrid.SearchRequest>(), It.IsAny<DataGrid.AssetType>(), It.IsAny<DataGrid.AssetVisibility>()))
                .ThrowsAsync(new InvalidOperationException(errorMessage));

            var qualifier = AssetQualifier.CreateForApacheCassandra("host");

            var reader = this.CreateDataAssetReader(provider, principle);
            await Assert.ThrowsAsync<MissingWritePermissionException>(() => reader.FindByQualifierAsync(filterCriteria, qualifier, false)).ConfigureAwait(false);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                // Make the paging values predictable to avoid random test failures.
                this.Fixture.Customize<Mock<IDataGridConfiguration>>(entity =>
                    entity.Do(x =>
                    {
                        x.SetupGet(m => m.DefaultPageSize).Returns(10);
                        x.SetupGet(m => m.MaxPageSize).Returns(20);
                        x.SetupGet(m => m.UseTransitionPropertiesAssetTypes).Returns(string.Empty);
                        x.SetupGet(m => m.UseSearchPropertiesAssetTypes).Returns(string.Empty);
                        x.SetupGet(m => m.UseMatchPropertiesAssetTypes).Returns(string.Empty);
                    }));

                this.Fixture.Customize<Mock<ICoreConfiguration>>(obj =>
                    obj.Do(x => x.SetupGet(m => m.MaxPageSize).Returns(5)));
            }
        }

        /// <summary>
        /// NOTE: You can't use the ValidDataAttribute and <c>InlineAutoMoqData</c> in the same test method. All kinds of chaos ensues because
        /// it generates test runs for each combination of <c>AutoMoqData</c> based tags and <c>InlineAutoMoqData</c> tags, and still runs instances w/o 
        /// the customized mocks.
        /// The fix, as best as I could figure, is to make a new attribute that includes the <c>AutoMoqData</c> class as a parameter. This seems
        /// to only invoke the test once per tag and performs the customizations on each one. Why that isn't the default is left to the reader to contemplate
        /// in their spare time.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Moq is valid.")]
        public class CombinedInlineDataAttribute : InlineAutoMoqDataAttribute
        {
            public CombinedInlineDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
        private DataAssetReader CreateDataAssetReader(
            DataAssetProvider provider,
            AuthenticatedPrincipal principle)
        {
            var fixture = new Fixture().EnableAutoMoq();
            var authProvider = fixture.Freeze<Mock<IAuthorizationProvider>>();
            var sessionFactory = fixture.Freeze<Mock<ISessionFactory>>();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();

            return new DataAssetReader(authProvider.Object, provider, principle, sessionFactory.Object, eventFactory.Object);
        }
    }
}
