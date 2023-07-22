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

    public class TeamGroupClientTest
    {
        [Theory(DisplayName = "Verify ServiceTreeClient.ReadTeamGroupWithExtendedProperties."), AutoMoqData]
        public async Task VerifyReadTeamGroupWithExtendedPropertiesAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "TeamGroup.ReadByIdResponse.json"));

                stub
                    .Setup(m => m.Execute("ServiceGroup.GetAuthorizationsAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "ServiceGroup.GetAuthorizationsResponse.json"));

                stub
                    .Setup(m => m.Execute("TeamGroup.GetHierarchyAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "TeamGroup.GetHierarchyResponse.json"));

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

                var result = await client.ReadTeamGroupWithExtendedProperties(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(Guid.Parse("05d1a8fe-cb2a-4fba-8ca2-dbc1fc0e25aa"), result.Response.Id);
                Assert.Equal(ServiceTreeLevel.TeamGroup, result.Response.Level);
                Assert.Equal("Member Experiences and Engagement", result.Response.Name);
                Assert.Equal("VSO Service Line", result.Response.Description);
                Assert.Equal(new[] { "dastc", "jeffblak", "romand" }, result.Response.AdminUserNames);
                Assert.Equal(Guid.Parse("ecf3aa0e-d692-4300-b19b-9e1cd96d74ab"), result.Response.DivisionId);
                Assert.Equal("WDG", result.Response.DivisionName);
                Assert.Equal(Guid.Parse("efab8c59-3cd4-4eb2-8e8a-77b687f93843"), result.Response.OrganizationId);
                Assert.Equal("Universal Store", result.Response.OrganizationName);
                Assert.Equal(Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d"), result.Response.ServiceGroupId);
                Assert.Equal("Membership", result.Response.ServiceGroupName);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.FindByNameAsync.")]
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

                var result = await client.TeamGroups.FindByNameAsync("test", new RequestContext(), filterResults).ConfigureAwait(false);
                                
                Assert.Contains(result.Response, x => x.Name == "Member Experiences and Engagement" && x.Level == ServiceTreeLevel.TeamGroup);
                
                if (filterResults)
                {
                    Assert.DoesNotContain(result.Response, x => x.Name == "Membership" && x.Level == ServiceTreeLevel.ServiceGroup);
                }
                else
                {
                    Assert.Contains(result.Response, x => x.Name == "Membership" && x.Level == ServiceTreeLevel.ServiceGroup);
                }
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.GetAuthorizationsAsync."), AutoMoqData]
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

                var result = await client.TeamGroups.GetAuthorizationsAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal("dastc", result.Response.First().Id);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.GetHierarchyAsync."), AutoMoqData]
        public async Task VerifyGetHierarchyAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.GetHierarchyAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "TeamGroup.GetHierarchyResponse.json"));

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

                var result = await client.TeamGroups.GetHierarchyAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(4, result.Response.Count());
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync."), AutoMoqData]
        public async Task VerifyReadByIdAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "TeamGroup.ReadByIdResponse.json"));

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

                var result = await client.TeamGroups.ReadByIdAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal("Member Experiences and Engagement", result.Response.Name);
                Assert.NotNull(result.Response.ServiceGroupId);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync NotFoundError old."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound_Old(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "TeamGroup.ReadByIdNotFoundError.json"));

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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.TeamGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync NotFoundError."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.TeamGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync Unknown 400 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown400Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "TeamGroup.ReadByIdUnknownError.json"));

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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.TeamGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync empty error response."), AutoMoqData(true)]
        public async Task VerifyReadByIdAsync_EmptyErrorResponse(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.TeamGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(string.Empty, result.Message);
            }
        }

        [Theory(DisplayName = "Verify TeamGroups.ReadByIdAsync Unknown 500 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown500Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("TeamGroup.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.InternalServerError, "TeamGroup.ReadByIdUnknownError.json"));

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

                var result = await Assert.ThrowsAsync<ServiceFault>(() => client.TeamGroups.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }
    }
}
