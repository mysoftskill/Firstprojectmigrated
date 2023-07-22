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

    public class TransferRequestsControllerTest
    {
        [Theory(DisplayName = "Verify TransferRequests.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            TransferRequest transferRequest,
            [Frozen] Core.TransferRequest coreTransferRequest,
            Mock<ITransferRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<ITransferRequestWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/transferRequests",
                            TestHelper.Serialize(transferRequest)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<TransferRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.TransferRequest>()), Times.Once);

                Likenesses.ForTransferRequest(actual).ShouldEqual(coreTransferRequest);
            }
        }

        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task VerifyReadByIdWithExpansions(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            Mock<ITransferRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ITransferRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/transferRequests('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<TransferRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task VerifyReadByIdWithoutExpansions(Mock<ITransferRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ITransferRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/transferRequests('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<TransferRequest>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(actual.TrackingDetails);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query (sourceOwnerId).")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=sourceOwnerId eq '{0}'", true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false)]
        public async Task VerifyReadBySourceOwnerId(
            string filterString,
            bool hasOwnerIdFilter,
            Guid ownerId,
            Mock<ITransferRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ITransferRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/transferRequests" + string.Format(filterString, ownerId);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<TransferRequest>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.TransferRequestFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.SourceOwnerId.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query (targetOwnerId).")]
        [AutofixtureCustomizations.InlineTypeCorrections("?$filter=targetOwnerId eq '{0}'", true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false)]
        public async Task VerifyReadByTargetOwnerId(
            string filterString,
            bool hasOwnerIdFilter,
            Guid ownerId,
            Mock<ITransferRequestReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<ITransferRequestReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var request = $"api/v2/transferRequests" + string.Format(filterString, ownerId);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                // Act.
                await TestHelper.DeserializePagingResponse<TransferRequest>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.TransferRequestFilterCriteria> verify = filter =>
                {
                    if (hasOwnerIdFilter)
                    {
                        Assert.Equal(ownerId, filter.TargetOwnerId.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify), Core.ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify TransferRequests.Delete."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyDelete(
            Guid id,
            string etag,
            Mock<ITransferRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<ITransferRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/transferRequests('{id}')")
                        .AddHeader("If-Match", etag)
                        .SendAsync("DELETE")
                        .ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                writer.Verify(m => m.DeleteAsync(id, etag), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify TransferRequests.Approve."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyApprove(
            Guid id,
            string etag,
            Mock<ITransferRequestWriter> writer)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<ITransferRequestWriter>();
            };

            etag = $"\"{etag}\""; // ETags must be quoted strings.

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act.
                var response =
                    await server
                        .CreateRequest($"api/v2/transferRequests('{id}')/v2.approve")
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