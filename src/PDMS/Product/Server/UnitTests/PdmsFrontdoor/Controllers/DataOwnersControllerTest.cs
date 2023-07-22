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
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    public class DataOwnersControllerTest
    {
        [Theory(DisplayName = "Verify DataOwners.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            DataOwner dataOwner,
            [Frozen] Core.DataOwner coreDataOwner,
            Mock<IDataOwnerWriter> writer)
        {
            dataOwner.DataAgents = null;
            dataOwner.AssetGroups = null;

            Action<ContainerBuilder> registration = cb =>
            {
                coreDataOwner.DataAgents = null;
                coreDataOwner.AssetGroups = null;
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.DataOwner>()), Times.Once);

                Likenesses.ForDataOwner(actual).ShouldEqual(coreDataOwner);
            }
        }

        [Theory(DisplayName = "When service does not exist, then DataOwners.Create throws ServiceTreeNotFoundError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithServiceNotExisting(
            DataOwner dataOwner,
            Guid serviceId,
            Mock<IDataOwnerWriter> writer)
        {
            dataOwner.ServiceTree = new ServiceTree { ServiceId = serviceId.ToString() };
            dataOwner.DataAgents = null;
            dataOwner.AssetGroups = null;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();

                writer
                    .Setup(m => m.CreateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new ServiceNotFoundException(serviceId));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Contains("\"code\":\"ServiceTree\"", responseMessage);
                Assert.Contains($"\"code\":\"Service\"", responseMessage);
                Assert.Contains($"\"id\":\"{serviceId}\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When user does not have service tree write permission, then DataOwners.Create throws ServiceTreeNotAuthorizedError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithoutServiceTreeWritePermissions(
            DataOwner dataOwner,
            Guid serviceId,
            Mock<IDataOwnerWriter> writer,
            string userName)
        {
            dataOwner.ServiceTree = new ServiceTree { ServiceId = serviceId.ToString() };
            dataOwner.DataAgents = null;
            dataOwner.AssetGroups = null;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();

                writer
                    .Setup(m => m.CreateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new ServiceTreeMissingWritePermissionException(userName, serviceId.ToString(), "missingRole"));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("\"code\":\"ServiceTree\"", responseMessage);
                Assert.Contains($"\"code\":\"User\"", responseMessage);
                Assert.Contains($"\"userName\":\"{userName}\"", responseMessage);
                Assert.Contains($"\"serviceId\":\"{serviceId}\"", responseMessage);
                Assert.Contains("\"role\":\"missingRole\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When user does not have security group write permission, then DataOwners.Create throws SecurityGroupNotAuthorizedError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithoutSecurityGroupWritePermissions(
            DataOwner dataOwner,
            Guid serviceId,
            Mock<IDataOwnerWriter> writer,
            string userName,
            IEnumerable<IEnumerable<Guid>> securityGroupList)
        {
            dataOwner.ServiceTree = new ServiceTree { ServiceId = serviceId.ToString() };
            dataOwner.DataAgents = null;
            dataOwner.AssetGroups = null;

            var securityGroups = string.Join(";", securityGroupList.Select(x => string.Join(";", x)));

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();

                writer
                    .Setup(m => m.CreateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new SecurityGroupMissingWritePermissionException(userName, securityGroups, "missingRole"));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("\"code\":\"SecurityGroup\"", responseMessage);
                Assert.Contains($"\"code\":\"User\"", responseMessage);
                Assert.Contains($"\"userName\":\"{userName}\"", responseMessage);
                Assert.Contains($"\"securityGroups\":\"{securityGroups}\"", responseMessage);
                Assert.Contains("\"role\":\"missingRole\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When security group does not exist, then DataOwners.Create throws SecurityGroupNotFoundError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithSecurityGroupNotExisting(
            DataOwner dataOwner,
            Guid securityGroupId,
            Mock<IDataOwnerWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();

                writer
                    .Setup(m => m.CreateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new SecurityGroupNotFoundException(securityGroupId));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Contains("\"code\":\"SecurityGroup\"", responseMessage);
                Assert.Contains($"\"id\":\"{securityGroupId}\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When create with service tree and properties other than write security groups set, then DataOwners.Create throws MutuallyExclusiveError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreateWithMutuallyExclusiveViolated(
            DataOwner dataOwner,
            Guid serviceId,
            Mock<IDataOwnerWriter> writer)
        {
            dataOwner.ServiceTree = new ServiceTree { ServiceId = serviceId.ToString() };
            dataOwner.DataAgents = null;
            dataOwner.AssetGroups = null;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();

                writer
                    .Setup(m => m.CreateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new MutuallyExclusiveException("serviceTree", "name", "value", "message"));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataOwners",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains("\"code\":\"MutuallyExclusive\"", responseMessage);
                Assert.Contains("\"code\":\"InvalidValue\"", responseMessage);
                Assert.Contains("\"source\":\"serviceTree\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=serviceTree", Core.ExpandOptions.ServiceTree)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails,serviceTree", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.ServiceTree)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents", Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=assetGroups", Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=serviceTree&$expand=dataAgents", Core.ExpandOptions.ServiceTree | Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents,assetGroups", Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents,assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            [Frozen] Core.DataOwner coreDataOwner,
            Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreDataOwner.DataAgents = null;
                coreDataOwner.AssetGroups = null;
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithoutExpansions_Then_ExcludeAll(Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Null(actual.TrackingDetails);
                Assert.Null(actual.DataAgents);
                Assert.Null(actual.AssetGroups);
            }
        }

        [Theory(DisplayName = "When entity does not exist, then ReadById throws EntityNotFoundError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyReadByIdWithEntityNotExisting(Mock<IDataOwnerReader> reader, Guid id)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();

                reader
                    .Setup(m => m.ReadByIdAsync(id, It.IsAny<Core.ExpandOptions>()))
                    .Throws(new EntityNotFoundException(id, "DataOwner"));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{id}')").ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Contains("\"code\":\"Entity\"", responseMessage);
                Assert.Contains($"\"id\":\"{id}\"", responseMessage);
                Assert.Contains($"\"type\":\"DataOwner\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When ReadById is called with tracking details, then include tracking details."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithTrackingDetails_Then_IncludeTrackingDetails(Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{Guid.Empty}')?$select=trackingDetails").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.NotNull(actual.TrackingDetails);
                Assert.Null(actual.DataAgents);
                Assert.Null(actual.AssetGroups);
            }
        }

        [Theory(DisplayName = "When ReadById is called with data agents, then include data agents."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithDataAgents_Then_IncludeDataAgents(Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{Guid.Empty}')?$expand=dataAgents").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.NotNull(actual.DataAgents);
                Assert.Null(actual.AssetGroups);
            }
        }

        [Theory(DisplayName = "When ReadById is called with asset groups, then include asset groups."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithAssetGroups_Then_IncludeAssetGroups(Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners('{Guid.Empty}')?$expand=assetGroups").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Null(actual.DataAgents);
                Assert.NotNull(actual.AssetGroups);
            }
        }

        [Theory(DisplayName = "When ReadByFilters is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents", Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=assetGroups", Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents,assetGroups", Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents,assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        public async Task When_ReadByFiltersHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            [Frozen] IEnumerable<Core.DataOwner> coreDataOwners,
            Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreDataOwner in coreDataOwners)
                {
                    coreDataOwner.DataAgents = null;
                    coreDataOwner.AssetGroups = null;
                }

                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.DataOwnerFilterCriteria>(), expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters Pagination next link includes filter and expand information."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersRequiresPaging_Then_EnsureFilterInformationIsPersisted(
            Mock<IDataOwnerReader> reader,
            string name,
            IFixture fixture)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var dataOwners = fixture.Build<Core.DataOwner>()
                               .Without(t => t.DataAgents)
                               .Without(t => t.AssetGroups)
                               .CreateMany(3);

                Action<Models.V2.DataOwnerFilterCriteria> verify = f =>
                {
                    Assert.Equal(name, f.Name.Value);
                    Assert.Equal(StringComparisonType.Contains, f.Name.ComparisonType);
                };

                reader
                   .Setup(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.DataAgents))
                   .ReturnsAsync(new FilterResult<Core.DataOwner> { Values = dataOwners, Total = 5, Index = 0, Count = 3 });

                var request = $"api/v2/dataOwners?$select=id,dataAgents&$expand=dataAgents&$filter=contains(name,'{name}')&$top=3&$skip=0";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(
                    actual.NextLink.EndsWith($"api/v2/dataOwners?$select=id,dataAgents&$expand=dataAgents&$filter=contains(name,'{name}')&$top=3&$skip=3"),
                    $"Check url: {actual.NextLink}");
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters Pagination next link is null if no more entities."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersHasNoPages_Then_NextLinkIsNull(
            Mock<IDataOwnerReader> reader,
            IFixture fixture)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var dataOwners = fixture.Build<Core.DataOwner>()
                               .Without(t => t.DataAgents)
                               .Without(t => t.AssetGroups)
                               .CreateMany(3);

                reader
                   .Setup(m => m.ReadByFiltersAsync(It.IsAny<Models.V2.DataOwnerFilterCriteria>(), Core.ExpandOptions.None))
                   .ReturnsAsync(new FilterResult<Core.DataOwner> { Values = dataOwners, Total = 3, Index = 0, Count = 3 });

                var request = $"api/v2/dataOwners";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(actual.NextLink);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters filter mapping.")]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/divisionId eq 'test'", "DivisionId", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/divisionName eq 'test'", "DivisionName", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/organizationId eq 'test'", "OrganizationId", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/organizationName eq 'test'", "OrganizationName", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/serviceGroupId eq 'test'", "ServiceGroupId", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/serviceGroupName eq 'test'", "ServiceGroupName", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/teamGroupId eq 'test'", "TeamGroupId", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/teamGroupName eq 'test'", "TeamGroupName", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/serviceId eq 'test'", "ServiceId", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/serviceName eq 'test'", "ServiceName", "test", StringComparisonType.Equals)]
        [AutofixtureCustomizations.InlineTypeCorrections("serviceTree/divisionId eq null", "DivisionId", null, StringComparisonType.EqualsCaseSensitive)]
        public async Task VerifyFilterMapping(
            string filterString,
            string filterPropertyName,
            string filterValue,
            StringComparisonType comparisonType,
            Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/dataOwners?$filter={filterString}";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Action<Core.DataOwnerFilterCriteria> verify = f =>
                {
                    var type = f.ServiceTree.GetType();
                    var filter = type.GetProperty(filterPropertyName).GetValue(f.ServiceTree) as StringFilter;
                    Assert.Equal(filterValue, filter.Value);
                    Assert.Equal(comparisonType, filter.ComparisonType);
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None));
            }
        }

        [Theory(DisplayName = "When FindByAuthenticatedUser is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents", Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=assetGroups", Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$expand=dataAgents,assetGroups", Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails&$expand=dataAgents,assetGroups", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.DataAgents | Core.ExpandOptions.AssetGroups)]
        public async Task When_FindByAuthenticatedUserHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            [Frozen] IEnumerable<Core.DataOwner> coreDataOwners,
            Mock<IDataOwnerReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreDataOwner in coreDataOwners)
                {
                    coreDataOwner.DataAgents = null;
                    coreDataOwner.AssetGroups = null;
                }

                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataOwners/v2.findByAuthenticatedUser{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.FindByAuthenticatedUserAsync(expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify FindByAuthenticatedUser Pagination next link is null if no more entities."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_FindByAuthenticatedUserHasNoPages_Then_NextLinkIsNull(
            Mock<IDataOwnerReader> reader,
            IFixture fixture)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataOwnerReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var dataOwners = fixture.Build<Core.DataOwner>()
                               .Without(t => t.DataAgents)
                               .Without(t => t.AssetGroups)
                               .CreateMany(3);

                reader
                   .Setup(m => m.FindByAuthenticatedUserAsync(Core.ExpandOptions.None))
                   .ReturnsAsync(dataOwners);

                var request = $"api/v2/dataOwners/v2.findByAuthenticatedUser";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(actual.NextLink);
            }
        }

        [Theory(DisplayName = "Verify DataOwners.Update."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdate(
            DataOwner dataOwner,
            [Frozen] Core.DataOwner coreDataOwner,
            Mock<IDataOwnerWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreDataOwner.DataAgents = null;
                coreDataOwner.AssetGroups = null;
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/dataOwners('{dataOwner.Id}')",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.UpdateAsync(It.IsAny<Core.DataOwner>()), Times.Once);

                Likenesses.ForDataOwner(actual).ShouldEqual(coreDataOwner);
            }
        }

        [Theory(DisplayName = "When DataOwners.Update is called with non-matching ETag, then ETagMismatchError is thrown."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdateEntityWithMismatchedEtag(
            Mock<IDataOwnerWriter> wirter,
            DataOwner dataOwner,
            Guid etag)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(wirter.Object).As<IDataOwnerWriter>();

                wirter
                    .Setup(m => m.UpdateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new ETagMismatchException("ETag mismatch.", null, etag.ToString()));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/dataOwners('{dataOwner.Id}')",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
                Assert.Contains("\"code\":\"ETagMismatch\"", responseMessage);
                Assert.Contains($"\"value\":\"{etag.ToString()}\"", responseMessage);
            }
        }

        [Theory(DisplayName = "When DataOwners.Update is called with invalid characters, then BadArgument:InvalidValue:InvalidCharacter is thrown."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdateEntityWithInvalidCharacter(
            Mock<IDataOwnerWriter> wirter,
            string name,
            string value,
            string message,
            DataOwner dataOwner)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(wirter.Object).As<IDataOwnerWriter>();

                wirter
                    .Setup(m => m.UpdateAsync(It.IsAny<Core.DataOwner>()))
                    .Throws(new InvalidCharacterException(name, value, message));
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/dataOwners('{dataOwner.Id}')",
                            TestHelper.Serialize(dataOwner)).ConfigureAwait(false);

                string responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains("\"code\":\"BadArgument\"", responseMessage);
                Assert.Contains("\"code\":\"InvalidValue\"", responseMessage);
                Assert.Contains("\"code\":\"UnsupportedCharacter\"", responseMessage);
                Assert.Contains($"\"value\":\"{value}\"", responseMessage);
                Assert.Contains($"\"target\":\"{name}\"", responseMessage);
                Assert.Contains($"\"message\":\"{message}\"", responseMessage);
            }
        }

        [Theory(DisplayName = "Verify DataOwners.ReplaceServiceId."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyReplaceServiceId(
            DataOwner dataOwner,
            [Frozen] Core.DataOwner coreDataOwner,
            Mock<IDataOwnerWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreDataOwner.DataAgents = null;
                coreDataOwner.AssetGroups = null;
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            $"api/v2/dataOwners('{dataOwner.Id}')/v2.replaceServiceId",
                            TestHelper.Serialize(new ActionParameter { Value = dataOwner })).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.ReplaceServiceIdAsync(It.IsAny<Core.DataOwner>()), Times.Once);

                Likenesses.ForDataOwner(actual).ShouldEqual(coreDataOwner);
            }
        }

        [Theory(DisplayName = "Verify DataOwners.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<IDataOwnerWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataOwnerWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/dataOwners('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag), Times.Once);
            }
        }

        public class ActionParameter
        {
            public DataOwner Value { get; set; }
        }
    }
}