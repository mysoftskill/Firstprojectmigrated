namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.ClientSdks.ServiceTreeTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test calls to Service Tree.
    /// </summary>
    [TestClass]
    public class ServiceTreeClientTests
    {
        private const string MeePrivacyServiceTreeid = "eda3dc03-5654-43a9-a6fa-468706c89c97";
        private const string MeePrivacyServicePrefix = "MEE Privacy";

        // Query ServiceTree with ServiceGroup.NotFoundError.
        [TestMethod]
        public Task ServiceGroup_NotFoundError()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var id = Guid.NewGuid();
                var data = await Assert.ThrowsExceptionAsync<NotFoundError>(() => client.ServiceGroups.ReadByIdAsync(id, requestContext)).ConfigureAwait(false);
                Assert.AreEqual(id, data.Id);
            });
        }

        // Query ServiceTree with TeamGroup.NotFoundError
        [TestMethod]
        public Task TeamGroup_NotFoundError()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var id = Guid.NewGuid();
                var data = await Assert.ThrowsExceptionAsync<NotFoundError>(() => client.TeamGroups.ReadByIdAsync(id, requestContext)).ConfigureAwait(false);
                Assert.AreEqual(id, data.Id);
            });
        }

        // Query ServiceTree with ServiceGroup.GetAuthorizations
        [TestMethod]
        public Task ServiceGroup_GetAuthorizationsNotFound()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var id = Guid.NewGuid();
                var data = await Assert.ThrowsExceptionAsync<NotFoundError>(() => client.ServiceGroups.GetAuthorizationsAsync(id, requestContext)).ConfigureAwait(false);
                Assert.AreEqual(id, data.Id);
            });
        }

        // Query ServiceTree with Service.NotFoundError
        [TestMethod]
        public Task Service_NotFoundError()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var id = Guid.NewGuid();
                var data = await Assert.ThrowsExceptionAsync<NotFoundError>(() => client.Services.ReadByIdAsync(id, requestContext)).ConfigureAwait(false);
                Assert.AreEqual(id, data.Id);
            });
        }

        // Query ServiceTree with Service.FindByName
        [TestMethod]
        public Task Service_FindByName()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var data = await client.Services.FindByNameAsync(MeePrivacyServicePrefix, requestContext).ConfigureAwait(false);
                Assert.IsNotNull(data.Response);
                Assert.IsTrue(data.Response.Count() > 0);
            });
        }

        // Query ServiceTree with Service.GetServiceById
        [TestMethod]
        public Task Service_GetServiceById()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var data = await client.Services.ReadByIdAsync(Guid.Parse(MeePrivacyServiceTreeid), requestContext).ConfigureAwait(false);
                Assert.IsNotNull(data.Response, "Response is empty");
                Assert.IsTrue(data.Response.Name.Contains(MeePrivacyServicePrefix));
            });
        }

        // Query ServiceTree with Service.FindByAuthenticatedUserAsync
        [TestMethod]
        public Task Service_FindByAuthenticatedUser()
        {
            return this.Execute(async (client, requestContext) =>
            {
                var data = await client.Services.FindByAuthenticatedUserAsync(requestContext).ConfigureAwait(false);
                Assert.IsNotNull(data.ResponseContent);
            });
        }

        private Task Execute(Func<IServiceTreeClient, RequestContext, Task> action)
        {
            return action(TestSetup.ServiceTreeClientInstance, TestSetup.ServiceTreeRequestContext);
        }
    }
}
