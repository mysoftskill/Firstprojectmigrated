namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Newtonsoft.Json;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    using Core = Models.V2;

    public class AssetGroupsControllerTest
    {
        [Theory(DisplayName = "Verify AssetGroups.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            AssetGroup assetGroup,
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupWriter> writer)
        {
            assetGroup.DeleteAgent = null;
            assetGroup.ExportAgent = null;
            assetGroup.AccountCloseAgent = null;
            assetGroup.Inventory = null;
            assetGroup.DataAssets = null;
            assetGroup.Owner = null;

            Action<ContainerBuilder> registration = cb =>
            {
                coreAssetGroup.DeleteAgent = null;
                coreAssetGroup.ExportAgent = null;
                coreAssetGroup.AccountCloseAgent = null;
                coreAssetGroup.Inventory = null;
                coreAssetGroup.DataAssets = null;
                coreAssetGroup.Owner = null;
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/assetGroups",
                            TestHelper.Serialize(assetGroup)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.AssetGroup>()), Times.Once);

                Likenesses.ForAssetGroup(actual).ShouldEqual(coreAssetGroup);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups Create for AzureDocumentDB."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithAzureDocumentDB(
            AssetGroup assetGroup,
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupWriter> writer)
        {
            var docDBQualifier = AssetQualifier.CreateForAzureDocumentDB("myaccount", "mydatabase", "mycollection");

            assetGroup.DeleteAgent = null;
            assetGroup.ExportAgent = null;
            assetGroup.AccountCloseAgent = null;
            assetGroup.Inventory = null;
            assetGroup.DataAssets = null;
            assetGroup.Owner = null;
            assetGroup.Qualifier = docDBQualifier.Value;
            coreAssetGroup.QualifierParts = docDBQualifier.Properties;

            Action<ContainerBuilder> registration = cb =>
            {
                coreAssetGroup.DeleteAgent = null;
                coreAssetGroup.ExportAgent = null;
                coreAssetGroup.AccountCloseAgent = null;
                coreAssetGroup.Inventory = null;
                coreAssetGroup.DataAssets = null;
                coreAssetGroup.Owner = null;
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/assetGroups",
                            TestHelper.Serialize(assetGroup)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.AssetGroup>()), Times.Once);

                Likenesses.ForAssetGroup(actual).ShouldEqual(coreAssetGroup);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.Create for API Asset Type."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithAPI(
            AssetGroup assetGroup,
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupWriter> writer)
        {
            var apiQualifier = AssetQualifier.CreateForAPI("https://hostname.com", "/Path", "PUT");

            assetGroup.DeleteAgent = null;
            assetGroup.ExportAgent = null;
            assetGroup.AccountCloseAgent = null;
            assetGroup.Inventory = null;
            assetGroup.DataAssets = null;
            assetGroup.Owner = null;
            assetGroup.Qualifier = apiQualifier.Value;
            coreAssetGroup.QualifierParts = apiQualifier.Properties;

            Action<ContainerBuilder> registration = cb =>
            {
                coreAssetGroup.DeleteAgent = null;
                coreAssetGroup.ExportAgent = null;
                coreAssetGroup.AccountCloseAgent = null;
                coreAssetGroup.Inventory = null;
                coreAssetGroup.DataAssets = null;
                coreAssetGroup.Owner = null;
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/assetGroups",
                            TestHelper.Serialize(assetGroup)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.AssetGroup>()), Times.Once);

                Likenesses.ForAssetGroup(actual).ShouldEqual(coreAssetGroup);
            }
        }

        [Theory(DisplayName = "When the TFS tracking uri is not valid, then AssetGroups.CreateAsync throws an InvalidArgumentError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task When_TfsTrackingUriIsNotValid_Then_ThrowInvalidArgumentError(
            Mock<IAssetGroupWriter> writer,
            AssetGroup assetGroup,
            AssetGroupVariant assetGroupVariant)
        {
            assetGroupVariant.TfsTrackingUris = assetGroupVariant.TfsTrackingUris.Concat(new[] { "invalid uri" });
            assetGroup.Variants = assetGroup.Variants.Concat(new[] { assetGroupVariant });

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/assetGroups",
                            TestHelper.Serialize(assetGroup)).ConfigureAwait(false);

                // Act.
                var responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains("\"code\":\"BadArgument\"", responseMessage);
                Assert.Contains("\"code\":\"InvalidValue\"", responseMessage);
                Assert.Contains("\"target\":\"tfsTrackingUris\"", responseMessage);
                Assert.Contains("\"value\":\"invalid uri\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When asset group exist or not, then AssetGroups.GetComplianceStateByAssetQualifier returns IsCompliant=true"), AutofixtureCustomizations.TypeCorrections()]
        public async Task When_FoundAssetGroupWithDeleteAgent_Then_ReturnCorrectComplianceState(
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupReader> reader,
            AssetQualifier assetQualifier)
        {
            coreAssetGroup.ComplianceState = new Core.ComplianceState() { IsCompliant = true, IncompliantReason = null };
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                string url = $"/api/v2/assetGroups/v2.findByAssetQualifier(qualifier=@value)/complianceState?@value='{assetQualifier.Value}'";

                HttpResponseMessage response = await server.HttpClient.GetAsync(url).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<ComplianceState>(response).ConfigureAwait(false);

                // Assert.
                Likenesses.ForComplianceState(actual).ShouldEqual(coreAssetGroup.ComplianceState);
            }
        }

        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=deleteAgent", Core.ExpandOptions.DeleteAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=exportAgent", Core.ExpandOptions.ExportAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAssets", Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=deleteAgent", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DeleteAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=exportAgent", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.ExportAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAssets", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=deleteAgent,exportAgent,dataAssets", Core.ExpandOptions.DeleteAgent | Core.ExpandOptions.ExportAgent | Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=deleteAgent,exportAgent,dataAssets", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DeleteAgent | Core.ExpandOptions.ExportAgent | Core.ExpandOptions.DataAssets)]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreAssetGroup.DeleteAgent = null;
                coreAssetGroup.ExportAgent = null;
                coreAssetGroup.AccountCloseAgent = null;
                coreAssetGroup.DataAssets = null;
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithoutExpansions_Then_ExcludeAll(Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Null(actual.TrackingDetails);
                Assert.Null(actual.DeleteAgent);
                Assert.Null(actual.ExportAgent);
                Assert.Null(actual.AccountCloseAgent);
                Assert.Null(actual.DataAssets);
            }
        }

        [Theory(DisplayName = "When ReadById is for an AzureDocumentDB asset, it returns it normalized"), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdForAzureDocumentDB(
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupReader> reader)
        {
            var docDBQualifier = AssetQualifier.CreateForAzureDocumentDB("myaccount", "mydatabase", "mycollection");
            coreAssetGroup.Qualifier = docDBQualifier;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(docDBQualifier.Value, actual.Qualifier);
            }
        }

        [Theory(DisplayName = "When ReadById is for an API asset, it returns it normalized"), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdForAPI(
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupReader> reader)
        {
            var apiQualifier = AssetQualifier.CreateForAPI("HTTPS://HOSTNAME.COM", "/Path", "put");
            coreAssetGroup.Qualifier = apiQualifier;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(apiQualifier.Value, actual.Qualifier);
            }
        }

        [Theory(DisplayName = "When ReadById is called with data assets, then include data assets."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithDataAssets_Then_IncludeDataAssets(Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')?$expand=dataAssets").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.NotNull(actual.DataAssets);
                Assert.Null(actual.DeleteAgent);
                Assert.Null(actual.ExportAgent);
                Assert.Null(actual.AccountCloseAgent);
            }
        }

        [Theory(DisplayName = "When ReadById is called with delete agent, then include delete agent."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithDeleteAgent_Then_IncludeDeleteAgent([Frozen] Core.AssetGroup assetGroup, Core.DeleteAgent deleteAgent, Mock<IAssetGroupReader> reader)
        {
            assetGroup.DeleteAgent = deleteAgent;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')?$expand=deleteAgent").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.NotNull(actual.DeleteAgent);
                Assert.Null(actual.ExportAgent);
                Assert.Null(actual.AccountCloseAgent);
                Assert.Null(actual.DataAssets);
            }
        }

        [Theory(DisplayName = "When ReadById is called with export agent, then include export agent."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithExportAgent_Then_IncludeExportAgent([Frozen] Core.AssetGroup assetGroup, Core.DeleteAgent deleteAgent, Mock<IAssetGroupReader> reader)
        {
            assetGroup.DeleteAgent = deleteAgent;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups('{Guid.Empty}')?$expand=exportAgent").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.NotNull(actual.ExportAgent);
                Assert.Null(actual.DeleteAgent);
                Assert.Null(actual.AccountCloseAgent);
                Assert.Null(actual.DataAssets);
            }
        }

        [Theory(DisplayName = "When ReadByFilters is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=deleteAgent", Core.ExpandOptions.DeleteAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=exportAgent", Core.ExpandOptions.ExportAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAssets", Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=deleteAgent", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DeleteAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=exportAgent", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.ExportAgent)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAssets", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=deleteAgent,exportAgent,dataAssets", Core.ExpandOptions.DeleteAgent | Core.ExpandOptions.ExportAgent | Core.ExpandOptions.DataAssets)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=deleteAgent,exportAgent,dataAssets", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DeleteAgent | Core.ExpandOptions.ExportAgent | Core.ExpandOptions.DataAssets)]
        public async Task When_ReadByFiltersHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            [Frozen] IEnumerable<Core.AssetGroup> coreAssetGroups,
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreAssetGroup in coreAssetGroups)
                {
                    coreAssetGroup.DeleteAgent = null;
                    coreAssetGroup.DataAssets = null;
                }

                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/assetGroups{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.AssetGroupFilterCriteria>(), expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}' and deleteAgentId eq '{1}'", true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}'", true, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=deleteAgentId eq '{1}'", false, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false, false)]
        public async Task When_ReadByFilters_Then_MapPropertiesCorrectly(
            string filterString,
            bool hasOwnerIdFilter,
            bool hasDeleteAgentIdFilter,
            Guid ownerId,
            Guid deleteAgentId,
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups" + string.Format(filterString, ownerId, deleteAgentId);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.AssetGroupFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.OwnerId.Value);
                    }

                    if (hasDeleteAgentIdFilter)
                    {
                        Assert.Equal(deleteAgentId, filter.DeleteAgentId.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters using equals comparison for qualifier string."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersWithEqualsQualifier_Then_IncludedEmptyPropertiesAsNull(
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups?$filter=qualifier eq 'AssetType=CosmosStructuredStream;PhysicalCluster=pc;VirtualCluster=vc'";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.AssetGroupFilterCriteria> verify = filter =>
                {
                    Assert.Equal("pc", filter.Qualifier["PhysicalCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["PhysicalCluster"].ComparisonType);
                    Assert.Equal("vc", filter.Qualifier["VirtualCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["VirtualCluster"].ComparisonType);
                    Assert.Null(filter.Qualifier["RelativePath"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.EqualsCaseSensitive, filter.Qualifier["RelativePath"].ComparisonType);
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters using contains comparison for partial qualifier string."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersWithContainsQualifier_Then_IncludedOnlyProvidedValues(
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups?$filter=contains(qualifier,'AssetType=CosmosStructuredStream;PhysicalCluster=pc;VirtualCluster=vc')";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.AssetGroupFilterCriteria> verify = filter =>
                {
                    Assert.Equal("pc", filter.Qualifier["PhysicalCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["PhysicalCluster"].ComparisonType);
                    Assert.Equal("vc", filter.Qualifier["VirtualCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["VirtualCluster"].ComparisonType);
                    Assert.False(filter.Qualifier.ContainsKey("RelativePath"));
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters using contains comparison for fully qualified string."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersWithContainsQualifier_Then_MapCorrectly(
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups?$filter=contains(qualifier,'AssetType=CosmosStructuredStream;PhysicalCluster=pc;VirtualCluster=vc;RelativePath=/test')";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.AssetGroupFilterCriteria> verify = filter =>
                {
                    Assert.Equal("pc", filter.Qualifier["PhysicalCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["PhysicalCluster"].ComparisonType);
                    Assert.Equal("vc", filter.Qualifier["VirtualCluster"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.Equals, filter.Qualifier["VirtualCluster"].ComparisonType);
                    Assert.Equal("/test/", filter.Qualifier["RelativePath"].Value);
                    Assert.Equal(Models.Filters.StringComparisonType.StartsWithCaseSensitive, filter.Qualifier["RelativePath"].ComparisonType);
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}'")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=deleteAgentId eq '{0}'")]
        public async Task When_ReadByFiltersHasInvalidGuid_Then_ReturnCallerError(
            string filterString,
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups" + string.Format(filterString, "badGuid");

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.Contains("BadArgument", jsonString);
                Assert.Contains("InvalidValue", jsonString);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersWithOrExpression_Then_MapCorrectly(
            Guid ownerId,
            Guid deleteAgentId,
            Guid exportAgentId,
            Guid accountCloseAgentId,
            Mock<IAssetGroupReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IAssetGroupReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/assetGroups?$filter=((ownerId eq '{ownerId}' and deleteAgentId eq '{deleteAgentId}')";
                request += $"or (ownerId eq '{ownerId}' and exportAgentId eq '{exportAgentId}'))";
                request += $"or (accountCloseAgentId eq '{accountCloseAgentId}')";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Action<Models.Filters.CompositeFilterCriteria<Core.AssetGroup>> verify = filter =>
                {
                    var filterC = filter.FilterB as Core.AssetGroupFilterCriteria;
                    Assert.NotNull(filterC);
                    Assert.Null(filterC.OwnerId);
                    Assert.Equal(accountCloseAgentId, filterC.AccountCloseAgentId);
                    Assert.Null(filterC.DeleteAgentId);
                    Assert.Null(filterC.ExportAgentId);

                    var _ = filter.FilterA as Models.Filters.CompositeFilterCriteria<Core.AssetGroup>;
                    var filterB = _.FilterB as Core.AssetGroupFilterCriteria;
                    Assert.NotNull(filterB);
                    Assert.Equal(ownerId, filterB.OwnerId);
                    Assert.Equal(exportAgentId, filterB.ExportAgentId);
                    Assert.Null(filterB.DeleteAgentId);
                    Assert.Null(filterB.AccountCloseAgentId);

                    var filterA = _.FilterA as Core.AssetGroupFilterCriteria;
                    Assert.NotNull(filterA);
                    Assert.Equal(ownerId, filterA.OwnerId);
                    Assert.Equal(deleteAgentId, filterA.DeleteAgentId);
                    Assert.Null(filterA.ExportAgentId);
                    Assert.Null(filterA.AccountCloseAgentId);
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.Update."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdate(
            AssetGroup assetGroup,
            [Frozen] Core.AssetGroup coreAssetGroup,
            Mock<IAssetGroupWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreAssetGroup.DeleteAgent = null;
                coreAssetGroup.ExportAgent = null;
                coreAssetGroup.AccountCloseAgent = null;
                coreAssetGroup.Inventory = null;
                coreAssetGroup.DataAssets = null;
                coreAssetGroup.Owner = null;
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/assetGroups('{assetGroup.Id}')",
                            TestHelper.Serialize(assetGroup)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.UpdateAsync(It.IsAny<Core.AssetGroup>()), Times.Once);

                Likenesses.ForAssetGroup(actual).ShouldEqual(coreAssetGroup);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<IAssetGroupWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/assetGroups('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.SetAgentRelationships."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifySetAgentRelationships(
            SetAgentRelationshipParameters apiRequest,
            Core.SetAgentRelationshipResponse coreResponse,
            Mock<IAssetGroupWriter> writer)
        {
            Action<Core.SetAgentRelationshipParameters> verify = x => Likenesses.ForSetAgentRelationshipParameters(apiRequest).ShouldEqual_(x);

            writer.Setup(m => m.SetAgentRelationshipsAsync(It.IsAny<Core.SetAgentRelationshipParameters>())).ReturnsAsync(coreResponse);

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/assetGroups/v2.setAgentRelationships",
                            TestHelper.Serialize(apiRequest)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<SetAgentRelationshipResponse>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(actual);
                Likenesses.ForSetAgentRelationshipResponse(coreResponse).ShouldEqual(actual);

                writer.Verify(m => m.SetAgentRelationshipsAsync(Is.Value(verify)), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.RemoveVariants."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyRemoveVariants(
            Guid id,
            IEnumerable<Guid> variantIds,
            string etag,
            Mock<IAssetGroupWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            etag = $"\"{etag}\"";
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var ids = JsonConvert.SerializeObject(variantIds);

                var body = new StringContent(
                    $"{{variantIds: { ids }}}",
                    System.Text.Encoding.UTF8,
                    "application/json");

                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/assetGroups('{id}')/v2.removeVariants")
                        .AddHeader("If-Match", etag)
                        .And(message => message.Content = body)
                        .SendAsync("POST")
                        .ConfigureAwait(false);

                var actual = await TestHelper.Deserialize<AssetGroup>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(actual);

                writer.Verify(m => m.RemoveVariantsAsync(id, variantIds, etag), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.RemoveVariants fails without etag."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyRemoveVariantsFailsNoEtag(
            Guid id,
            IEnumerable<Guid> variantIds,
            Mock<IAssetGroupWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var ids = JsonConvert.SerializeObject(variantIds);

                var body = new StringContent(
                    $"{{variantIds: { ids }}}",
                    System.Text.Encoding.UTF8,
                    "application/json");

                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/assetGroups('{id}')/v2.removeVariants")
                        .And(message => message.Content = body)
                        .SendAsync("POST")
                        .ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.RemoveVariantsAsync(id, variantIds, null), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify AssetGroups.RemoveVariants fails with invalid Guids."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyRemoveVariantsFailsInvalidGuids(
            Guid id,
            string etag,
            IEnumerable<string> variantIds,
            Mock<IAssetGroupWriter> writer)
        {
            writer.Setup(x => x.RemoveVariantsAsync(It.IsAny<Guid>(), Is.Value<IEnumerable<Guid>>(y => y.ToList()), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException());
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IAssetGroupWriter>();
            };

            variantIds = variantIds.Select(x => "badGuid" + x);

            etag = $"\"{etag}\"";
            var serverContainer = TestServer.CreateDependencies(registration);
            using (var server = TestServer.Create(serverContainer))
            {
                var ids = JsonConvert.SerializeObject(variantIds);

                var body = new StringContent(
                    $"{{variantIds: { ids }}}",
                    System.Text.Encoding.UTF8,
                    "application/json");

                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/assetGroups('{id}')/v2.removeVariants")
                        .AddHeader("If-Match", etag)
                        .And(message => message.Content = body)
                        .SendAsync("POST")
                        .ConfigureAwait(false);

                var responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains("\"code\":\"BadArgument\"", responseMessage);
                Assert.Contains("\"target\":\"variantIds\"", responseMessage);
                Assert.Contains("\"code\":\"InvalidValue\"", responseMessage);
            }
        }
    }
}
