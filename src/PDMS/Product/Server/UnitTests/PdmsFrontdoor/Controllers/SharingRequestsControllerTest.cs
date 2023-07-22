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

    public class SharingRequestsControllerTest
    {
        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            Mock<ISharingRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ISharingRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/sharingRequests('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<SharingRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithoutExpansions_Then_ExcludeAll(Mock<ISharingRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ISharingRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/sharingRequests('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<SharingRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(actual.TrackingDetails);
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
            Mock<ISharingRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ISharingRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/sharingRequests" + string.Format(filterString, ownerId, deleteAgentId);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<SharingRequest>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.SharingRequestFilterCriteria> verify = filter =>
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

        [Theory(DisplayName = "Verify SharingRequests.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<ISharingRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<ISharingRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/sharingRequests('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify SharingRequests.Approve."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyApprove(
            Guid id,
            string etag,
            Mock<ISharingRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<ISharingRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/sharingRequests('{id}')/v2.approve")
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