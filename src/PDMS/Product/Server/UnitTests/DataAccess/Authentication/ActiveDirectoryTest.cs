namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class ActiveDirectoryTest
    {
        [Theory(DisplayName = "When username is null, then return empty list."), ConfigData]
        public async Task When_TestUserNameNull_Then_UseEmptyList(
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            principal.UserId = null;

            var results = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Empty(results);
        }

        [Theory(DisplayName = "When test overrides are enabled and the user is the test account, then return fixed values."), ConfigData]
        public async Task When_TestUserValid_Then_UseFixedValues(
            [Frozen] Mock<IAzureActiveDirectoryProviderConfig> configuration,
            IEnumerable<Guid> securityGroups,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            configuration.SetupGet(m => m.EnableIntegrationTestOverrides).Returns(true);
            configuration.SetupGet(m => m.IntegrationTestSecurityGroups).Returns(securityGroups.Select(s => s.ToString()).ToList());

            principal.UserId = configuration.Object.IntegrationTestUserName;

            var results = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.True(securityGroups.SequenceEqual(results));
        }

        [Theory(DisplayName = "When test overrides are enabled and the user is not the test account, then return graph values."), ConfigData]
        public async Task When_TestUserNotValid_Then_UseGraphValues(
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> graphResponse,
            [Frozen] Mock<IAzureActiveDirectoryProviderConfig> configuration,
            IEnumerable<Guid> securityGroups,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            graphResponse.Setup(x => x.CurrentPage).Returns(new List<DirectoryObject>());
            graphResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            configuration.SetupGet(m => m.EnableIntegrationTestOverrides).Returns(true);
            configuration.SetupGet(m => m.IntegrationTestSecurityGroups).Returns(securityGroups.Select(s => s.ToString()).ToList());

            var results = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Empty(results);
        }

        [Theory(DisplayName = "When the graph returns an empty list, then security groups is empty."), ConfigData]
        public async Task When_GraphReturnsEmpty_Then_GroupsAreEmpty(
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> graphResponse,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            graphResponse.Setup(x => x.CurrentPage).Returns(new List<DirectoryObject>());
            graphResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            var securityGroups = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Empty(securityGroups);
        }

        [Theory(DisplayName = "When the graph returns a single result, then map to guids."), ConfigData]
        public async Task When_GraphReturnsSingleResult_Then_MapToGuids(
            Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest> graphRequest,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> graphResponse,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder> userClient,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            IEnumerable<DirectoryObject> knownSecurityGroups = new List<DirectoryObject>()
                    { new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = true } };

            graphResponse.Object.Add(knownSecurityGroups.First());

            var securityGroupGuids = knownSecurityGroups.Select(group => Guid.Parse(group.Id)).ToList();

            graphRequest.Setup(m => m.GetAsync()).Returns(Task.FromResult(graphResponse.Object));
            graphResponse.Setup(x => x.CurrentPage).Returns(knownSecurityGroups.ToList());
            graphResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            userClient.Setup(m => m.Request()).Returns(graphRequest.Object);

            var securityGroups = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);
            Assert.True(securityGroupGuids.SequenceEqual(securityGroups));
        }

        [Theory(DisplayName = "When graph returns multiple results, then expand the pages."), ConfigData]
        public async Task When_GraphReturnsMultipleResults_Then_ExpandPages(
            Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest> secondRequest,
            Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> secondResponse,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> firstResponse,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            IEnumerable<DirectoryObject> page1 = new List<DirectoryObject>()
                { new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = true } };

            IEnumerable<DirectoryObject> page2 = new List<DirectoryObject>()
                { new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = true } };

            firstResponse.Object.Add(page1.First());
            secondResponse.Object.Add(page2.First());

            var securityGroupGuids = page1.Concat(page2).Select(group => Guid.Parse(group.Id)).ToList();

            firstResponse.Setup(x => x.CurrentPage).Returns(page1.ToList());
            firstResponse.Setup(x => x.NextPageRequest).Returns(secondRequest.Object);

            secondRequest.Setup(x => x.GetAsync()).ReturnsAsync(secondResponse.Object);
            secondResponse.Setup(x => x.CurrentPage).Returns(page2.ToList());
            secondResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            var securityGroups = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.True(securityGroupGuids.SequenceEqual(securityGroups));
        }

        [Theory(DisplayName = "Verify instrumentation occurs for each page request."), ConfigData]
        public async Task VerifyInstrumentation(
            [Frozen] Mock<ISession> session,
            [Frozen] Mock<ISessionFactory> sessionFactory,
            IEnumerable<DirectoryObject> page1,
            IEnumerable<DirectoryObject> page2,
            Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest> secondRequest,
            Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> secondResponse,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> firstResponse,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            firstResponse.Setup(x => x.CurrentPage).Returns(page1.ToList());
            firstResponse.Setup(x => x.NextPageRequest).Returns(secondRequest.Object);

            secondRequest.Setup(x => x.GetAsync()).ReturnsAsync(secondResponse.Object);
            secondResponse.Setup(x => x.CurrentPage).Returns(page2.ToList());
            secondResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            var securityGroups = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            sessionFactory.Verify(m => m.StartSession("ActiveDirectory.GetTransitiveMemberOfAsync", SessionType.Outgoing), Times.Exactly(2));
            session.Verify(m => m.Done(SessionStatus.Success, It.Is<IUserTransitiveMemberOfCollectionWithReferencesPage>(x => x == firstResponse.Object)), Times.Exactly(1));
            session.Verify(m => m.Done(SessionStatus.Success, It.Is<IUserTransitiveMemberOfCollectionWithReferencesPage>(x => x == secondResponse.Object)), Times.Exactly(1));
        }

        [Theory(DisplayName = "Verify that groups without SecurityEnabled set are not retrieved."), ConfigData]
        public async Task VerifyTheCorrectSecurityGroupsAreRetrieved(
            [Frozen] IEnumerable<DirectoryObject> knownSecurityGroups,
            [Frozen] IEnumerable<DirectoryObject> nonSecurityGroups,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest> graphRequest,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesPage> graphResponse,
            [Frozen] Mock<IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder> userClient,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal)
        {
            knownSecurityGroups = new List<DirectoryObject>()
                    { new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = true } };

            nonSecurityGroups = new List<DirectoryObject>()
                    { new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = false } };

            var combinedList = knownSecurityGroups.Concat(nonSecurityGroups);

            graphResponse.Object.Add(knownSecurityGroups.First());
            graphResponse.Object.Add(nonSecurityGroups.First());
            graphRequest.Setup(m => m.GetAsync()).Returns(Task.FromResult(graphResponse.Object));
            graphResponse.Setup(x => x.CurrentPage).Returns(combinedList.ToList());
            graphResponse.Setup(x => x.NextPageRequest).Returns(() => null);

            userClient.Setup(m => m.Request()).Returns(graphRequest.Object);

            var securityGroups = await activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.True(knownSecurityGroups.Select(x => Guid.Parse(x.Id)).SequenceEqual(securityGroups));
        }

        [Theory(DisplayName = "When SecurityGroupIdExists finds a security group, then return true."), ConfigData]
        public async Task When_SecurityGroupIdExistsFindSecurityGroup_Then_ReturnTrue(
            [Frozen] Mock<IGroupRequest> graphRequest,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal,
            Guid securityGroupId)
        {
            graphRequest.Setup(m => m.GetAsync()).ReturnsAsync(new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = true });

            var result = await activeDirectory.SecurityGroupIdExistsAsync(principal, securityGroupId).ConfigureAwait(false);

            Assert.True(result);
        }

        [Theory(DisplayName = "When SecurityGroupIdExists finds a distribution group, then return false."), ConfigData]
        public async Task When_SecurityGroupIdExistsFindDistributionGroup_Then_ReturnFalse(
            [Frozen] Mock<IGroupRequest> graphRequest,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal,
            Guid securityGroupId)
        {
            graphRequest.Setup(m => m.GetAsync()).ReturnsAsync(new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = false });

            var result = await activeDirectory.SecurityGroupIdExistsAsync(principal, securityGroupId).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "When test overrides enabled, then SecurityGroupIdExists returns true."), ConfigData]
        public async Task When_TestOverridesEnabled_Then_SecurityGroupIdExistsReturnsTrue(
            [Frozen] Mock<IAzureActiveDirectoryProviderConfig> configuration,
            [Frozen] Mock<IGroupRequest> graphRequest,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal,
            Guid securityGroupId)
        {
            configuration.Setup(m => m.EnableIntegrationTestOverrides).Returns(true);
            graphRequest.Setup(m => m.GetAsync()).ReturnsAsync(new Group { Id = Guid.NewGuid().ToString(), SecurityEnabled = false });

            var result = await activeDirectory.SecurityGroupIdExistsAsync(principal, securityGroupId).ConfigureAwait(false);

            Assert.True(result);
        }

        [Theory(DisplayName = "When SecurityGroupIdExists receives NotFound, then return false."), ConfigData]
        public async Task When_SecurityGroupIdExistsFindsNotFound_Then_ReturnFalse(
            [Frozen] Mock<IGroupRequest> graphRequest,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal,
            Guid securityGroupId)
        {
            var exn = new ServiceException(new Graph.Error(), null, System.Net.HttpStatusCode.NotFound);

            graphRequest.Setup(m => m.GetAsync()).ThrowsAsync(exn);

            var result = await activeDirectory.SecurityGroupIdExistsAsync(principal, securityGroupId).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "When SecurityGroupIdExists receives exception, then rethrow."), ConfigData]
        public async Task When_SecurityGroupIdExistsHitsException_Then_ReThrow(
            [Frozen] Mock<IGroupRequest> graphRequest,
            ActiveDirectory activeDirectory,
            AuthenticatedPrincipal principal,
            Guid securityGroupId)
        {
            var exn = new ServiceException(new Graph.Error(), null, System.Net.HttpStatusCode.BadRequest);

            graphRequest.Setup(m => m.GetAsync()).ThrowsAsync(exn);

            await Assert.ThrowsAsync<ServiceException>(() => activeDirectory.SecurityGroupIdExistsAsync(principal, securityGroupId)).ConfigureAwait(false);
        }

        private IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder GetResponse(IEnumerable<DirectoryObject> dirObjs)
        {
            var mock = new Mock<IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder>();
            var mock2 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest>();
            var mock3 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesPage>();

            mock3.Setup(x => x.CurrentPage).Returns(dirObjs.ToList());
            mock3.Setup(x => x.NextPageRequest).Returns(() => null);
            mock2.Setup(m => m.GetAsync()).ReturnsAsync(mock3.Object);
            mock.Setup(m => m.Request(null)).Returns(mock2.Object);

            return mock.Object;
        }

        private IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder GetResponse(IEnumerable<Group> value, IEnumerable<Group> value2 = null)
        {
            return this.GetResponse(value.Select(x => (DirectoryObject)x), value2?.Select(x => (DirectoryObject)x));
        }

        private IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder GetResponse(IEnumerable<DirectoryObject> value, IEnumerable<DirectoryObject> value2 = null)
        {
            var mock = new Mock<IUserTransitiveMemberOfCollectionWithReferencesRequestBuilder>();
            var mock2 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest>();
            var mock3 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesPage>();

            var mock4 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesRequest>();
            var mock5 = new Mock<IUserTransitiveMemberOfCollectionWithReferencesPage>();

            if (value2 != null)
            {
                mock5.Setup(x => x.CurrentPage).Returns(value2.ToList());
                mock5.Setup(x => x.NextPageRequest).Returns(() => null);
                mock4.Setup(m => m.GetAsync()).ReturnsAsync(mock5.Object);
            }

            mock3.Setup(x => x.CurrentPage).Returns(value.ToList());
            mock3.Setup(x => x.NextPageRequest).Returns(() => value2 == null ? null : mock4.Object);
            mock2.Setup(m => m.GetAsync()).ReturnsAsync(mock3.Object);

            mock.Setup(m => m.Request(null)).Returns(mock2.Object);

            return mock.Object;
        }

        #region AutoFixture Customizations
        public class ConfigDataAttribute : AutoMoqDataAttribute
        {
            public ConfigDataAttribute(bool disableRecursionCheck = false)
                : base(disableRecursionCheck)
            {
                this.Fixture.Customize<Mock<IAzureActiveDirectoryProviderConfig>>(entity =>
                    entity
                    .Do(m =>
                    {
                        m.SetupGet(x => x.EnableIntegrationTestOverrides).Returns(false);
                        m.SetupGet(x => x.IntegrationTestUserName).Returns("testUser");
                    }));

                this.Fixture.Customize<DirectoryObject>(entity =>
                    entity
                    .With(x => x.Id, Guid.NewGuid().ToString())
                    .With(x => x.ODataType, "microsoft.graph.group")
                    );

                this.Fixture.Customize<Group>(entity =>
                    entity
                    .With(x => x.Id, Guid.NewGuid().ToString())
                    .With(x => x.SecurityEnabled, true)
                    );
            }
        }
        #endregion
    }
}