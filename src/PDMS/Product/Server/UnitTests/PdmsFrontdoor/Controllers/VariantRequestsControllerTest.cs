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

    public class VariantRequestsControllerTest
    {
        [Theory(DisplayName = "Verify VariantRequests.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            VariantRequest variantRequest,
            [Frozen] Core.VariantRequest coreVariantRequest,
            Mock<IVariantRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantRequestWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/variantRequests",
                            TestHelper.Serialize(variantRequest)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.VariantRequest>()), Times.Once);

                Likenesses.ForVariantRequest(actual).ShouldEqual(coreVariantRequest);
            }
        }

        [Theory(DisplayName = "Verify VariantRequests.Update."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdate(
            VariantRequest variantRequest,
            [Frozen] Core.VariantRequest coreVariantRequest,
            Mock<IVariantRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantRequestWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/variantRequests('{variantRequest.Id}')",
                            TestHelper.Serialize(variantRequest)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.UpdateAsync(It.IsAny<Core.VariantRequest>()), Times.Once);

                Likenesses.ForVariantRequest(actual).ShouldEqual(coreVariantRequest);
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
            Mock<IVariantRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/variantRequests('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithoutExpansions_Then_ExcludeAll(Mock<IVariantRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/variantRequests('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(actual.TrackingDetails);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=ownerId eq '{0}'", true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false)]
        public async Task When_ReadByFilters_Then_MapPropertiesCorrectly(
            string filterString,
            bool hasOwnerIdFilter,
            Guid ownerId,
            Mock<IVariantRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/variantRequests" + string.Format(filterString, ownerId);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<VariantRequest>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.VariantRequestFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.OwnerId.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify VariantRequests.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<IVariantRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/variantRequests('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify VariantRequests.Approve."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyApprove(
            Guid id,
            string etag,
            Mock<IVariantRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/variantRequests('{id}')/v2.approve")
                        .AddHeader("If-Match", etag)
                        .SendAsync("POST")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.ApproveAsync(id, etag), Times.Once);
            }
        }
    }
}