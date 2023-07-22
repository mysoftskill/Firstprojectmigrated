namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Xunit;

    public class ServiceClientTest
    {
        [Theory(DisplayName = "Verify ServiceTreeClient.FindServicesByName."), AutoMoqData]
        public async Task VerifyServiceTreeClientFindServicessByNameAsync(Mock<IServiceTreeStub> stub, string name)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.FindByNameAsync", new object[] { "'" + name + "'" }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.FindByNameResponse.json"));

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

                var result = await client.FindServicesByName(name, new RequestContext()).ConfigureAwait(false);

                var s = result.Response.Single(x => x.Level == ServiceTreeLevel.Service && x.Name == "WPF Testing");
                Assert.Equal(Guid.Parse("8047a9f9-ace0-49ad-a9fc-e0ff16fc7000"), s.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceTreeClient.FindNodesByName."), AutoMoqData]
        public async Task VerifyServiceTreeClientFindNodesByNameAsync(Mock<IServiceTreeStub> stub, string name)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.FindByNameAsync", new object[] { "'" + name + "'" }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.FindByNameResponse.json"));

                stub
                    .Setup(m => m.Execute("ServiceGroupOrTeamGroup.FindByNameAsync", new object[] { name }))
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

                var result = await client.FindNodesByName(name, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(22, result.Response.Count()); // There are lots of services in the mock response data.

                var sg = result.Response.Single(x => x.Level == ServiceTreeLevel.ServiceGroup);
                Assert.Equal(Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d"), sg.Id);
                Assert.Equal("Membership", sg.Name);

                var tg = result.Response.Single(x => x.Level == ServiceTreeLevel.TeamGroup);
                Assert.Equal(Guid.Parse("05d1a8fe-cb2a-4fba-8ca2-dbc1fc0e25aa"), tg.Id);
                Assert.Equal("Member Experiences and Engagement", tg.Name);

                var s = result.Response.Single(x => x.Level == ServiceTreeLevel.Service && x.Name == "WPF Testing");
                Assert.Equal(Guid.Parse("8047a9f9-ace0-49ad-a9fc-e0ff16fc7000"), s.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceTreeClient.ReadServiceWithExtendedProperties."), AutoMoqData]
        public async Task VerifyReadServiceWithExtendedPropertiesAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.ReadByIdResponse.json"));

                stub
                    .Setup(m => m.Execute("TeamGroup.GetHierarchyAsync", new object[] { Guid.Parse("05d1a8fe-cb2a-4fba-8ca2-dbc1fc0e25aa") }))
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

                var result = await client.ReadServiceWithExtendedProperties(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(Guid.Parse("eda3dc03-5654-43a9-a6fa-468706c89c97"), result.Response.Id);
                Assert.Equal(ServiceTreeLevel.Service, result.Response.Level);
                Assert.Equal("MEE Privacy Service", result.Response.Name);
                Assert.Equal("MEE Privacy Service - Consolidate Privacy experiences across the company SE Team Alias : meese\nFeature PM : Mouna Sidi Hida\nPlatform : Modern\nRoadmap : Roadmap Timeline : RS1\nVIP’s : Azure Subscriptions : Yes\nPatching Methods : AP/IMP\nFQDN :", result.Response.Description);
                Assert.Equal(new[] { "dastc", "jeffblak", "jessp", "pplaiste", "sbragg" }, result.Response.AdminUserNames);
                Assert.Equal(Guid.Parse("ecf3aa0e-d692-4300-b19b-9e1cd96d74ab"), result.Response.DivisionId);
                Assert.Equal("WDG", result.Response.DivisionName);
                Assert.Equal(Guid.Parse("efab8c59-3cd4-4eb2-8e8a-77b687f93843"), result.Response.OrganizationId);
                Assert.Equal("Universal Store", result.Response.OrganizationName);
                Assert.Equal(Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d"), result.Response.ServiceGroupId);
                Assert.Equal("Membership", result.Response.ServiceGroupName);
                Assert.Equal(Guid.Parse("05d1a8fe-cb2a-4fba-8ca2-dbc1fc0e25aa"), result.Response.TeamGroupId);
                Assert.Equal("Member Experiences and Engagement", result.Response.TeamGroupName);
            }
        }

        [Theory(DisplayName = "Verify ServiceTreeClient.ReadServiceWithExtendedPropertiesWithoutTeamGroup."), AutoMoqData]
        public async Task VerifyReadServiceWithExtendedPropertiesAsync_WithoutTeamGroup(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.ReadByIdResponseWithoutTeamGroup.json"));

                stub
                    .Setup(m => m.Execute("ServiceGroup.GetHierarchyAsync", new object[] { Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d") }))
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

                var result = await client.ReadServiceWithExtendedProperties(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal(Guid.Parse("eda3dc03-5654-43a9-a6fa-468706c89c97"), result.Response.Id);
                Assert.Equal(ServiceTreeLevel.Service, result.Response.Level);
                Assert.Equal("MEE Privacy Service", result.Response.Name);
                Assert.Equal("MEE Privacy Service - Consolidate Privacy experiences across the company SE Team Alias : meese\nFeature PM : Mouna Sidi Hida\nPlatform : Modern\nRoadmap : Roadmap Timeline : RS1\nVIP’s : Azure Subscriptions : Yes\nPatching Methods : AP/IMP\nFQDN :", result.Response.Description);
                Assert.Equal(new[] { "dastc", "jeffblak", "jessp", "pplaiste", "sbragg" }, result.Response.AdminUserNames);
                Assert.Equal(Guid.Parse("ecf3aa0e-d692-4300-b19b-9e1cd96d74ab"), result.Response.DivisionId);
                Assert.Equal("WDG", result.Response.DivisionName);
                Assert.Equal(Guid.Parse("efab8c59-3cd4-4eb2-8e8a-77b687f93843"), result.Response.OrganizationId);
                Assert.Equal("Universal Store", result.Response.OrganizationName);
                Assert.Equal(Guid.Parse("a81edd6c-bee1-4dd3-830c-ad027dc9134d"), result.Response.ServiceGroupId);
                Assert.Equal("Membership", result.Response.ServiceGroupName);
                Assert.Null(result.Response.TeamGroupId);
                Assert.Null(result.Response.TeamGroupName);
            }
        }

        [Theory(DisplayName = "Verify Services.FindByNameAsync."), AutoMoqData]
        public async Task VerifyFindByNameAsync(Mock<IServiceTreeStub> stub)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.FindByNameAsync", new object[] { "'test'" }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.FindByNameResponse.json"));

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

                var result = await client.Services.FindByNameAsync("test", new RequestContext()).ConfigureAwait(false);

                Assert.Contains(result.Response, x => x.Name == "WPF Testing");
                Assert.Contains(result.Response, x => x.Name == "PegasusTest");
            }
        }

        [Theory(DisplayName = "Verify Services.FindByNameAsync returning malformed response."), AutoMoqData(true)]
        public async Task VerifyFindByNameAsync_MalformedResponse(Mock<IServiceTreeStub> stub)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.FindByNameAsync", new object[] { "'testMalformed'" }))
                    .Returns(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent("Bad Response", Encoding.UTF8, "application/json")
                    });

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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.Services.FindByNameAsync("testMalformed", new RequestContext())).ConfigureAwait(false);

                Assert.Equal("Bad Response", result.Message);
            }
        }

        [Theory(DisplayName = "Verify Services.FindByAuthenticatedUserAsync."), AutoMoqData]
        public async Task VerifyFindByAuthenticatedUserAsync(Mock<IServiceTreeStub> stub)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.FindByAuthenticatedUserAsync", null))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.FindByAuthenticatedUserResponse.json"));

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

                var result = await client.Services.FindByAuthenticatedUserAsync(new RequestContext()).ConfigureAwait(false);

                Assert.Contains(result.Response, x => x.Name == "MEE Privacy Service");
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync."), AutoMoqData]
        public async Task VerifyReadByIdAsync(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.OK, "Service.ReadByIdResponse.json"));

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

                var result = await client.Services.ReadByIdAsync(id, new RequestContext()).ConfigureAwait(false);

                Assert.Equal("MEE Privacy Service", result.Response.Name);
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync NotFoundError old."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound_Old(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "Service.ReadByIdNotFoundError.json"));

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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.Services.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync NotFoundError."), AutoMoqData]
        public async Task VerifyReadByIdAsync_NotFound(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.Services.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(id, result.Id);
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync Unknown 400 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown400Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.BadRequest, "Service.ReadByIdUnknownError.json"));

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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.Services.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync empty error response."), AutoMoqData]
        public async Task VerifyReadByIdAsync_EmptyErrorResponse(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
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

                var result = await Assert.ThrowsAsync<NotFoundError>(() => client.Services.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal(string.Empty, result.Message);
            }
        }

        [Theory(DisplayName = "Verify Services.ReadByIdAsync Unknown 500 error."), AutoMoqData]
        public async Task VerifyReadByIdAsync_Unknown500Error(Mock<IServiceTreeStub> stub, Guid id)
        {
            Action<ContainerBuilder> serverRegistration = cb =>
            {
                stub
                    .Setup(m => m.Execute("Service.ReadByIdAsync", new object[] { id }))
                    .Returns(TestServer.LoadResponse(HttpStatusCode.InternalServerError, "Service.ReadByIdUnknownError.json"));

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

                var result = await Assert.ThrowsAsync<ServiceFault>(() => client.Services.ReadByIdAsync(id, new RequestContext())).ConfigureAwait(false);

                Assert.Equal("This is an unknown error from the service.", result.Message);
            }
        }
    }
}
