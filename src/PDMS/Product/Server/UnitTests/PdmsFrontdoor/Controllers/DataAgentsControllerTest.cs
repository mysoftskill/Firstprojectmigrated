namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.Testing;
    using Moq;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Core = Models.V2;

    public class DataAgentsControllerTest
    {
        [Theory(DisplayName = "Verify DataAgents.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            DataAgent dataAgent,
            [Frozen] Core.DataAgent coreDataAgent,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentWriter> writer,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            dataAgent.Owner = null;

            Action<ContainerBuilder> registration = cb =>
            {
                coreDataAgent.Owner = null;
                coreDataAgent.MigratingConnectionDetails = new Dictionary<Core.ReleaseState, Core.ConnectionDetail>();
                cb.RegisterInstance(writer.Object).As<IDataAgentWriter>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var x = TestHelper.Serialize(dataAgent);
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/dataAgents",
                            TestHelper.Serialize(dataAgent)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.DataAgent>()), Times.Once);

                Likenesses.ForDataAgent(actual).ShouldEqual(coreDataAgent);
            }
        }

        [Theory(DisplayName = "When CheckOwnerShip is called data agent info and SG info is fetched .")]
        [AutofixtureCustomizations.InlineTypeCorrections(Core.ExpandOptions.ServiceTree)]
        public async Task When_CheckOwnershipCalled_FetchAgentInfoAndSGInfo(
            Core.ExpandOptions expandOptions,
            Guid id,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDeleteAgentReader> reader,
            Mock<IDataOwnerReader> dataOwnerReader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDeleteAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(dataOwnerReader.Object).As<IDataOwnerReader>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents('{id}')/v2.DeleteAgent/v2.checkOwnership").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
                dataOwnerReader.Verify(m => m.FindByAuthenticatedUserAsync(expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When delete agent ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails,sharingEnabled", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadDeleteAgentByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            Core.DeleteAgent coreLegacyAgent,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDeleteAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                reader.Setup(m => m.ReadByIdAsync(It.IsAny<Guid>(), expandOptions)).ReturnsAsync(coreLegacyAgent);
                cb.RegisterInstance(reader.Object).As<IDeleteAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents('{id}')/v2.DeleteAgent{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DeleteAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When delete agent CalculateRegistrationStatus is called, then call business layer."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_DeleteAgentCalculateStatus_Then_CallBusinessLayer(
            Guid id,
            Core.AgentRegistrationStatus agentStatus,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDeleteAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                reader.Setup(m => m.CalculateRegistrationStatus(It.IsAny<Guid>())).ReturnsAsync(agentStatus);
                cb.RegisterInstance(reader.Object).As<IDeleteAgentReader>(); 
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents('{id}')/v2.DeleteAgent/v2.calculateRegistrationStatus").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<AgentRegistrationStatus>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.CalculateRegistrationStatus(id), Times.Once);

                Assert.Equal(agentStatus.Id.ToString(), actual.Id);
                Assert.Equal(agentStatus.OwnerId.ToString(), actual.OwnerId);
                Assert.Equal(agentStatus.IsComplete, actual.IsComplete);
                Assert.Equal(agentStatus.CapabilityStatus.ToString(), actual.CapabilityStatus.ToString());
                Assert.Equal(agentStatus.Capabilities.Select(x => x.Value), actual.Capabilities);
                Assert.Equal(agentStatus.EnvironmentStatus.ToString(), actual.EnvironmentStatus.ToString());
                Assert.Equal(agentStatus.Environments.Select(x => x.ToString()), actual.Environments.Select(x => x.ToString()));
                Assert.Equal(agentStatus.ProtocolStatus.ToString(), actual.ProtocolStatus.ToString());
                Assert.Equal(agentStatus.Protocols.Select(x => x.Value), actual.Protocols);
                Assert.Equal(agentStatus.AssetGroupsStatus.ToString(), actual.AssetGroupsStatus.ToString());

                for (int i = 0; i < agentStatus.AssetGroups.Count(); i++)
                {
                    var expected = agentStatus.AssetGroups.ElementAt(i);
                    var item = actual.AssetGroups.ElementAt(i);

                    Assert.Equal(expected.Id.ToString(), item.Id);
                    Assert.Equal(expected.OwnerId.ToString(), item.OwnerId);
                    Assert.Equal(expected.AssetsStatus.ToString(), item.AssetsStatus.ToString());
                    Assert.Equal(expected.IsComplete, item.IsComplete);
                    Assert.True(Likenesses.AreEquivalent(item.Qualifier, expected.Qualifier));

                    for (int j = 0; j < expected.Assets.Count(); j++)
                    {
                        var e = expected.Assets.ElementAt(j);
                        var a = item.Assets.ElementAt(j);

                        Assert.Equal(e.Id, a.Id);
                        Assert.Equal(e.IsComplete, a.IsComplete);
                        Assert.True(Likenesses.AreEquivalent(a.Qualifier, e.Qualifier));
                        Assert.Equal(e.DataTypeTagsStatus.ToString(), a.DataTypeTagsStatus.ToString());
                        Assert.Equal(e.DataTypeTags.Select(x => x.Name), a.DataTypeTags.Select(x => x.Name));
                        Assert.Equal(e.SubjectTypeTagsStatus.ToString(), a.SubjectTypeTagsStatus.ToString());
                        Assert.Equal(e.SubjectTypeTags.Select(x => x.Name), a.SubjectTypeTags.Select(x => x.Name));
                    }
                }
            }
        }

    [Theory(DisplayName = "When ReadByFilters is called, then parse properly."), AutofixtureCustomizations.TypeCorrections()]
        public async Task When_ReadByFilters_Then_ReturnEntitiesWithoutTrackingDetails(
            [Frozen] IEnumerable<Core.DataAgent> coreDataAgents,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreDataAgent in coreDataAgents)
                {
                    coreDataAgent.Owner = null;
                }

                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.DataAgentFilterCriteria>(), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadByFilters is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadByFiltersHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            [Frozen] IEnumerable<Core.DataAgent> coreDataAgents,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreDataAgent in coreDataAgents)
                {
                    coreDataAgent.Owner = null;
                }

                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.DataAgentFilterCriteria>(), expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When DeleteAgents.ReadByFilters is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=hasSharingRequests", Core.ExpandOptions.HasSharingRequests)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails,hasSharingRequests", Core.ExpandOptions.TrackingDetails | Core.ExpandOptions.HasSharingRequests)]
        public async Task When_DeleteAgentReadByFiltersHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            [Frozen] IEnumerable<Core.DeleteAgent> coreDeleteAgents,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDeleteAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                foreach (var coreDeleteAgent in coreDeleteAgents)
                {
                    coreDeleteAgent.Owner = null;
                }

                cb.RegisterInstance(reader.Object).As<IDeleteAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/dataAgents/v2.DeleteAgent{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DeleteAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.DeleteAgentFilterCriteria>(), expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters Pagination next link includes filter and expand information."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFiltersRequiresPaging_Then_EnsureFilterInformationIsPersisted(
            Mock<IDataAgentReader> reader,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory,
            string name,
            IFixture fixture)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var dataAgents = fixture.Build<Core.DeleteAgent>().CreateMany(3);

                reader
                   .Setup(m => m.ReadByFiltersAsync(It.IsAny<Models.V2.DataAgentFilterCriteria>(), Core.ExpandOptions.TrackingDetails))
                   .ReturnsAsync(new FilterResult<Core.DataAgent> { Values = dataAgents, Total = 5, Index = 0, Count = 3 });

                var request = $"api/v2/dataAgents?$select=id,trackingDetails&$filter=contains(name,'{name}')&$top=3&$skip=0";

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(
                    actual.NextLink.EndsWith($"api/v2/dataAgents?$select=id,trackingDetails&$filter=contains(name,'{name}')&$top=3&$skip=3"),
                    $"Check url: {actual.NextLink}");
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}' and name eq '{1}'", true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}'", true, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=name eq '{1}'", false, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false, false)]
        public async Task When_ReadByFilters_Then_MapPropertiesCorrectly(
            string filterString,
            bool hasOwnerIdFilter,
            bool hasNameFilter,
            Guid ownerId,
            string name,
            Mock<IDataAgentReader> reader,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/dataAgents" + string.Format(filterString, ownerId, name);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.DataAgentFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.OwnerId.Value);
                    }

                    if (hasNameFilter)
                    {
                        Assert.Equal(name, filter.Name.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters invalid query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}'")]
        public async Task When_ReadByFiltersHasInvalidGuid_Then_ReturnCallerError(
            string filterString,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/dataAgents" + string.Format(filterString, "badGuid");

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.Contains("BadArgument", jsonString);
                Assert.Contains("InvalidValue", jsonString);
            }
        }

        [Theory(DisplayName = "Verify DeleteAgent.ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}' and name eq '{1}' and sharingEnabled eq {2}", true, true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}' and name eq '{1}'", true, true, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}' and sharingEnabled eq {2}", true, false, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=name eq '{1}' and sharingEnabled eq {2}", false, true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false, false, false)]
        public async Task When_DeleteAgentReadByFilters_Then_MapPropertiesCorrectly(
            string filterString,
            bool hasOwnerIdFilter,
            bool hasNameFilter,
            bool hasSharingFilter,
            Guid ownerId,
            string name,
            bool sharingEnabled,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDeleteAgentReader> reader,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDeleteAgentReader>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/dataAgents/v2.DeleteAgent" + string.Format(filterString, ownerId, name, sharingEnabled.ToString().ToLower());

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataOwner>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.DeleteAgentFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.OwnerId.Value);
                    }
                    else
                    {
                        Assert.Null(filter.OwnerId);
                    }

                    if (hasNameFilter)
                    {
                        Assert.Equal(name, filter.Name.Value);
                    }
                    else
                    {
                        Assert.Null(filter.Name);
                    }

                    if (hasSharingFilter)
                    {
                        Assert.Equal(sharingEnabled, filter.SharingEnabled.Value);
                    }
                    else
                    {
                        Assert.Null(filter.SharingEnabled);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify DataAgents.Update."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdate(
            DataAgent dataAgent,
            [Frozen] Core.DataAgent coreDataAgent,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentWriter> writer,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                coreDataAgent.Owner = null;
                coreDataAgent.MigratingConnectionDetails = new Dictionary<Core.ReleaseState, Core.ConnectionDetail>();
                cb.RegisterInstance(writer.Object).As<IDataAgentWriter>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/dataAgents('{dataAgent.Id}')",
                            TestHelper.Serialize(dataAgent)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<DataAgent>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.UpdateAsync(It.IsAny<Core.DataAgent>()), Times.Once);

                Likenesses.ForDataAgent(actual).ShouldEqual(coreDataAgent);
            }
        }

        [Theory(DisplayName = "Verify DataAgents.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentWriter> writer,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataAgentWriter>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/dataAgents('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag, false, false), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify DataAgents.Delete with override."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDeleteWithOverride(
            Guid id,
            string etag,
            Mock<ICoreConfiguration> coreConfiguration,
            Mock<IDataAgentWriter> writer,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IDataAgentWriter>();
                cb.RegisterInstance(coreConfiguration.Object).As<ICoreConfiguration>();
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/dataAgents('{id}')/v2.override")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag, true, false), Times.Once);
            }
        }
    }
}