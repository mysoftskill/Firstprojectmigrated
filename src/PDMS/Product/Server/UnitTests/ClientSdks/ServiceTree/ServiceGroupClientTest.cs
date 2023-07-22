namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Xunit;

    public class ServiceGroupClientTest
    {
        [Theory(DisplayName = "Verify ServiceTreeClient.ReadServiceGroupWithExtendedProperties."), AutoMoqData]
        public async Task VerifyReadServiceGroupWithExtendedPropertiesAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.ReadByIdResponse.json"));

                stub
                    .Setup(m => m.Execute("ServiceGroup.GetAuthorizationsAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.GetAuthorizationsResponse.json"));

                stub
                    .Setup(m => m.Execute("ServiceGroup.GetHierarchyAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.GetHierarchyResponse.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await client.ReadServiceGroupWithExtendedProperties(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d"), result.Response.Id);
                Assert.Equal(ServiceTreeLevel.ServiceGroup, result.Response.Level);
                Assert.Equal("Membership", result.Response.Name);
                Assert.Equal("Represents the Universal Store high level team as mapped in Team Map", result.Response.Description);
                Assert.Equal(new[] { "dastc", "jeffblak", "romand" }, result.Response.AdminUserNames);                
                Assert.Equal(Guid.Parse("ecf3aa0e-d692-4300-b19b-9e1cd96d74ab"), result.Response.DivisionId);
                Assert.Equal("WDG", result.Response.DivisionName);
                Assert.Equal(Guid.Parse("efab8c59-3cd4-4eb2-8e8a-77b687f93843"), result.Response.OrganizationId);
                Assert.Equal("Universal Store", result.Response.OrganizationName);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.FindByNameAsync.")]
        [InlineAutoMoqData(true)]
        [InlineAutoMoqData(false)]
        public async Task VerifyFindByNameAsync(bool filterResults, Mock<IServiceTreeStub> stub)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroupOrTeamGroup.FindByNameAsync", new object[] { "test" }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroupOrTeamGroup.FindByNameResponse.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await client.ServiceGroups.FindByNameAsync("test", new RequestContext(), filterResults).ConfigureAwait(false);

                Assert.Contains(result.Response, x => x.Name == "Membership" && x.Level == ServiceTreeLevel.ServiceGroup);

                if (filterResults)
                {
                    Assert.DoesNotContain(result.Response, x => x.Name == "Member Experiences and Engagement" && x.Level == ServiceTreeLevel.TeamGroup);
                }
                else
                {
                    Assert.Contains(result.Response, x => x.Name == "Member Experiences and Engagement" && x.Level == ServiceTreeLevel.TeamGroup);
                }
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.GetAuthorizationsAsync."), AutoMoqData]
        public async Task VerifyGetAuthorizationsAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.GetAuthorizationsAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.GetAuthorizationsResponse.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await client.ServiceGroups.GetAuthorizationsAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal("dastc", result.Response.First().Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.GetHierarchyAsync."), AutoMoqData]
        public async Task VerifyGetHierarchyAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.GetHierarchyAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.GetHierarchyResponse.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await client.ServiceGroups.GetHierarchyAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(3, result.Response.Count());
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync."), AutoMoqData]
        public async Task VerifyReadByIdAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.ReadByIdResponse.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await client.ServiceGroups.ReadByIdAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal("Membership", result.Response.Name);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync NotFoundError old."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound_Old(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "ServiceGroup.ReadByIdNotFoundError.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.ServiceGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync NotFoundError."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.NotFound, null));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.ServiceGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync Unknown 400 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown400Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "ServiceGroup.ReadByIdUnknownError.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.ServiceGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync empty error response."), AutoMoqData]
        public async Task VerifyReadByIdAsync_EmptyErrorResponse(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, null));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.ServiceGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(string.Empty, result.Message);
            }
        }

        [Theory(DisplayName = "Verify ServiceGroups.ReadByIdAsync Unknown 500 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown500Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.InternalServerError, "ServiceGroup.ReadByIdUnknownError.json"));

                cb.RegisterInstance(stub.Object);
            };

            var serverContainer = TestServer.CreateDependencies(serverRegistration);

            using (var server = TestServer.Create(serverContainer))
            {
                Action<ContainerBuilder> clientRegistration = cb =>
                {
                    cb.RegisterInstance(new TestHttpServiceProxy(server)).Named<IHttpServiceProxy>("ServiceTree");
                };

                var clientContainer = TestServer.CreateDependencies(clientRegistration);

                var client = clientContainer.Resolve<IServiceTreeClient>();

                var result = await Assert.ThrowsAsync<ServiceFault>(() => client.ServiceGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }
    }
}
