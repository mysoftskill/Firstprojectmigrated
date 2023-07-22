namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataOwnerWriterTest
    {
        #region CreateAsync
        [Theory(DisplayName = "When CreateAsync is called, then the storage layer is called with the correct history item."), ValidData]
        public async Task When_CreateAsync_Then_StorageLayerCalledWithCorrectHistoryItem(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer)
        {
            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            storageWriter.Verify(m => m.CreateDataOwnerAsync(dataOwner), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called with alert contacts not set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutAlertContacts_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.AlertContacts = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("alertContacts", exn.ParamName);
        }

        [Theory(DisplayName = "Verify data owner ICM ConnectorId property.")]
        [InlineValidDataAttribute("00000000-0000-0000-0000-000000000000", true)]
        [InlineValidDataAttribute(null, false)]
        public async Task VerifyDataAgentConnectorId(
            string connectorId,
            bool shouldThrowException,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            if (connectorId == null)
            {
                dataOwner.Icm = null;
            }
            else
            {
                dataOwner.Icm.ConnectorId = Guid.Parse(connectorId);
            }

            if (shouldThrowException)
            {
                var exception = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);
                Assert.Equal("Empty Guid for ConnectorId is not allowed.", exception.Message);
                Assert.Equal("connectorId", exception.ParamName);
                Assert.Equal(exception.Value, Guid.Empty.ToString());
            }
            else
            {
                // No exceptions
                var owner = await writer.CreateAsync(dataOwner).ConfigureAwait(false);
                Assert.NotNull(owner);
            }
        }

        [Theory(DisplayName = "When CreateAsync is called with alert contacts empty, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithAlertContactsEmpty_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.AlertContacts = new System.Net.Mail.MailAddress[0];

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("alertContacts", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with tag security groups that do not exist, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWitInvalidTagSecurityGroups_Then_Fail(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), dataOwner.TagSecurityGroups.Last())).ReturnsAsync(false);

            await Assert.ThrowsAsync<SecurityGroupNotFoundException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with tag security groups that do exist, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithValidTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(true);

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called without tag security groups, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithoutTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.TagSecurityGroups = new Guid[0];

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with null tag security groups, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithNullTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.TagSecurityGroups = null;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with sharing request contacts that do exist, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithValidSharingRequestContacts_Then_Pass(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called without sharing request contacts, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithoutSharingRequestContacts_Then_Pass(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with null sharing request contacts, then pass."), ValidData]
        public async Task When_CreateAsyncCalledWithNullSharingRequestContacts_Then_Pass(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.SharingRequestContacts = null;

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with write security groups not set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutWriteSecurityGroups_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.WriteSecurityGroups = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("writeSecurityGroups", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with write security groups empty, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithWriteSecurityGroupsEmpty_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.WriteSecurityGroups = new Guid[0];

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("writeSecurityGroups", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with data agents, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithDataAgents_Then_Fail(
            IEnumerable<DataAgent> dataAgents,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.DataAgents = dataAgents;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("dataAgents", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset groups, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithAssetGroups_Then_Fail(
            IEnumerable<AssetGroup> assetGroups,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.AssetGroups = assetGroups;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("assetGroups", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but AlertContacts are not null, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButAlertContactsAreNotNull_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<System.Net.Mail.MailAddress> alertContacts)
        {
            dataOwner.AlertContacts = alertContacts;

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("alertContacts", exn.ParamName);
            Assert.Equal("serviceTree.serviceId", exn.Source);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but AnnouncementContacts are not null, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButAnnouncementContactsAreNotNull_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<System.Net.Mail.MailAddress> annoucementContacts)
        {
            dataOwner.AnnouncementContacts = annoucementContacts;

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("announcementContacts", exn.ParamName);
            Assert.Equal("serviceTree.serviceId", exn.Source);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but name is set, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndNameIsSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string name)
        {
            dataOwner.Name = name;

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("name", exn.ParamName);
            Assert.Equal(name, exn.Value);
            Assert.Equal("serviceTree.serviceId", exn.Source);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but description is set, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndDescriptioinIsSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string description)
        {
            dataOwner.Description = description;

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("description", exn.ParamName);
            Assert.Equal(description, exn.Value);
            Assert.Equal("serviceTree.serviceId", exn.Source);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but data agents are set, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButDataAgentsAreSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<DataAgent> dataAgents)
        {
            dataOwner.DataAgents = dataAgents;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("dataAgents", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but asset groups are set, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButAssetGroupsAreSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<AssetGroup> assetGroups)
        {
            dataOwner.AssetGroups = assetGroups;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("assetGroups", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree divisionId are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeDivisionIdSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string divisionId)
        {
            dataOwner.ServiceTree.DivisionId = divisionId;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.divisionId", exn.ParamName);
            Assert.Equal(divisionId, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree divisionName are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeDivisionNameSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string value)
        {
            dataOwner.ServiceTree.DivisionName = value;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.divisionName", exn.ParamName);
            Assert.Equal(value, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree serviceAdmins are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeServiceAdminsSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<string> serviceAdmins)
        {
            dataOwner.ServiceTree.ServiceAdmins = serviceAdmins;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceAdmins", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree organizationId are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeOrganizationIdSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string organizationId)
        {
            dataOwner.ServiceTree.OrganizationId = organizationId;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.organizationId", exn.ParamName);
            Assert.Equal(organizationId, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree organizationName are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeOrganizationNameSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string value)
        {
            dataOwner.ServiceTree.OrganizationName = value;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.organizationName", exn.ParamName);
            Assert.Equal(value, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree serviceGroupId are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeServiceGroupIdSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            Guid serviceGroupId)
        {
            dataOwner.ServiceTree.ServiceGroupId = serviceGroupId.ToString();
            dataOwner.ServiceTree.ServiceId = serviceGroupId.ToString();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceId", exn.ParamName);
            Assert.Equal(serviceGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree serviceGroupName are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeServiceGroupNameSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string value)
        {
            dataOwner.ServiceTree.ServiceGroupName = value;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceGroupName", exn.ParamName);
            Assert.Equal(value, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree teamGroupId are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeTeamGroupIdSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            Guid teamGroupId)
        {
            dataOwner.ServiceTree.TeamGroupId = teamGroupId.ToString();
            dataOwner.ServiceTree.ServiceId = teamGroupId.ToString();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceId", exn.ParamName);
            Assert.Equal(teamGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree teamGroupName are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeTeamGroupNameSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string value)
        {
            dataOwner.ServiceTree.TeamGroupName = value;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.teamGroupName", exn.ParamName);
            Assert.Equal(value, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service tree serviceName are set besides service id, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceTreeServiceNameSetBesidesServiceId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            string value)
        {
            dataOwner.ServiceTree.ServiceName = value;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceName", exn.ParamName);
            Assert.Equal(value, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service id is not a valid guid, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButServiceIdNotValid_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.ServiceTree.ServiceId = "Not a valid guid";

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceId", exn.ParamName);
            Assert.Equal("Not a valid guid", exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service id is an empty guid, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButServiceIdIsEmptyGuid_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.ServiceTree.ServiceId = Guid.Empty.ToString();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceId", exn.ParamName);
            Assert.Equal(Guid.Empty.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree but service id does not exist, then fail"), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeButServiceIdDoesNotExist_Then_Fail(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            Guid serviceId = Guid.Parse(dataOwner.ServiceTree.ServiceId);

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).Throws(new NotFoundError(serviceId));

            var exn = await Assert.ThrowsAsync<ServiceNotFoundException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(serviceId, exn.Id);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree and the service id exists, then set the data owner properties accordingly."), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceIdExists_Then_SetTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            [Frozen] DataOwner dataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult)
        {
            Guid serviceId = Guid.Parse(dataOwner.ServiceTree.ServiceId);

            var service = new Service
            {
                Id = serviceId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                ServiceGroupId = Guid.NewGuid(),
                ServiceGroupName = "sgName",
                TeamGroupId = Guid.NewGuid(),
                TeamGroupName = "tgName",
                Level = Client.ServiceTree.ServiceTreeLevel.Service
            };

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(service.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(service.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(service.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(service.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(service.ServiceGroupId.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(service.ServiceGroupName, result.ServiceTree.ServiceGroupName);
            Assert.Equal(service.TeamGroupId.ToString(), result.ServiceTree.TeamGroupId);
            Assert.Equal(service.TeamGroupName, result.ServiceTree.TeamGroupName);
            Assert.Equal(service.Id.ToString(), result.ServiceTree.ServiceId);
            Assert.Equal(service.Name, result.ServiceTree.ServiceName);
            Assert.Equal(service.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(service.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree and the team group id exists, then set the data owner properties accordingly."), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndTeamGroupIdExists_Then_SetTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            [Frozen] DataOwner dataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult)
        {
            Guid teamGroupId = Guid.Parse(dataOwner.ServiceTree.ServiceId);
            dataOwner.ServiceTree.TeamGroupId = dataOwner.ServiceTree.ServiceId;
            dataOwner.ServiceTree.ServiceId = null;

            var teamGroup = new TeamGroup
            {
                Id = teamGroupId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                ServiceGroupId = Guid.NewGuid(),
                ServiceGroupName = "sgName",
                Level = Client.ServiceTree.ServiceTreeLevel.TeamGroup
            };

            serviceClient.Setup(m => m.ReadTeamGroupWithExtendedProperties(teamGroupId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<TeamGroup>(httpResult, teamGroup));

            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("(TG) Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(teamGroup.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(teamGroup.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(teamGroup.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(teamGroup.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(teamGroup.ServiceGroupId.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(teamGroup.ServiceGroupName, result.ServiceTree.ServiceGroupName);
            Assert.Equal(teamGroup.Id.ToString(), result.ServiceTree.TeamGroupId);
            Assert.Equal(teamGroup.Name, result.ServiceTree.TeamGroupName);
            Assert.Null(result.ServiceTree.ServiceId);
            Assert.Null(result.ServiceTree.ServiceName);
            Assert.Equal(teamGroup.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(teamGroup.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree and the service group id exists, then set the data owner properties accordingly."), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceGroupIdExists_Then_SetTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            [Frozen] DataOwner dataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult)
        {
            Guid serviceGroupId = Guid.Parse(dataOwner.ServiceTree.ServiceId);
            dataOwner.ServiceTree.ServiceGroupId = dataOwner.ServiceTree.ServiceId;
            dataOwner.ServiceTree.ServiceId = null;

            var serviceGroup = new ServiceGroup
            {
                Id = serviceGroupId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                Level = Client.ServiceTree.ServiceTreeLevel.ServiceGroup
            };

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(serviceGroupId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<ServiceGroup>(httpResult, serviceGroup));

            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("(SG) Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(serviceGroup.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(serviceGroup.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(serviceGroup.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(serviceGroup.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(serviceGroup.Id.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(serviceGroup.Name, result.ServiceTree.ServiceGroupName);
            Assert.Null(result.ServiceTree.TeamGroupId);
            Assert.Null(result.ServiceTree.TeamGroupName);
            Assert.Null(result.ServiceTree.ServiceId);
            Assert.Null(result.ServiceTree.ServiceName);
            Assert.Equal(serviceGroup.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(serviceGroup.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree and the service id exists with null values, then set the data owner properties accordingly."), ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndServiceIdExistsWithNullValues_Then_SetTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            [Frozen] DataOwner dataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult)
        {
            Guid serviceId = Guid.Parse(dataOwner.ServiceTree.ServiceId);

            var service = new Service
            {
                Name = null,
                Description = null,
                AdminUserNames = new[] { "admin", "admin1" },
                ServiceGroupId = null,
                TeamGroupId = null
            };

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Null(result.Name);
            Assert.Null(result.Description);
            Assert.Null(result.ServiceTree.ServiceGroupId);
            Assert.Null(result.ServiceTree.TeamGroupId);
        }

        [Theory(DisplayName = "When data owner CreateAsync is called with service tree service id that already exists, then fail."), DataOwnerWriterTest.ValidServiceTreeData]
        public async Task When_DataOwnerCreateAsyncCalledWithServiceTreeServiceIdExists_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            FilterResult<DataOwner> existingOwners,
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IFixture fixture)
        {
            existingOwners.Values = fixture.CreateMany<DataOwner>();

            Action<DataOwnerFilterCriteria> filter = f =>
            {
                var expected = new DataOwnerFilterCriteria { ServiceTree = new ServiceTreeFilterCriteria { ServiceId = new StringFilter(dataOwner.ServiceTree.ServiceId, StringComparisonType.EqualsCaseSensitive) } };
                f
                .Likeness()
                .With(m => m.EntityType).EqualsWhen((src, dest) => src.EntityType.LikenessShouldEqual(dest.EntityType))
                .With(m => m.Name).EqualsWhen((src, dest) => src.Name == null)
                .With(m => m.ServiceTree).EqualsWhen((src, dest) => src.ServiceTree.ServiceId.LikenessShouldEqual(dest.ServiceTree.ServiceId))
                .ShouldEqual(expected);
            };

            entityReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(existingOwners);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.AlreadyExists, exn.ConflictType);
            Assert.Equal("serviceTree.serviceId", exn.Target);
            Assert.Equal(dataOwner.ServiceTree.ServiceId, exn.Value);
        }
        #endregion

        #region UpdateAsync
        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree, then the storage layer is called and returned."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree)
        {
            dataOwner.ServiceTree = updatedServiceTree;
            storageDataOwner.ServiceTree = currentServiceTree;

            storageDataOwner.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(storageDataOwner), Times.Once);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree, then the history item is created correctly."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_Then_HistoryItemCreatedCorrectly(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree)
        {
            dataOwner.ServiceTree = updatedServiceTree;
            storageDataOwner.ServiceTree = currentServiceTree;

            storageDataOwner.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            storageDataOwner.TrackingDetails = trackingDetails;

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(storageDataOwner), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree but data agents are set, then fail"), ValidServiceTreeData]
        public async Task When_UpdateAsyncCalledWithServiceTreeButDataAgentsAreSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<DataAgent> dataAgents,
            ServiceTree serviceTree)
        {
            dataOwner.ServiceTree = serviceTree;
            dataOwner.DataAgents = dataAgents;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("dataAgents", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree but asset groups are set, then fail"), ValidServiceTreeData]
        public async Task When_UpdateAsyncCalledWithServiceTreeButAssetGroupsAreSet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<AssetGroup> assetGroups,
            ServiceTree serviceTree)
        {
            dataOwner.ServiceTree = serviceTree;
            dataOwner.AssetGroups = assetGroups;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("assetGroups", exn.ParamName);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree while existing owner does not have service tree, then fail."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTreeButExistingOwnerDoesNotHaveServiceTree_Then_Fail(
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails,
            ServiceTree updatedServiceTree)
        {
            storageDataOwner.ServiceTree = null;
            dataOwner.ServiceTree = updatedServiceTree;

            storageDataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("serviceTree", exn.Target);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree and service id changed, then fail."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_And_ServiceIdIsChanged_Then_Fail(
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            [Frozen] DataOwner storageDataOwner,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree,
            IFixture fixture)
        {
            fixture.Inject<IValidator>(new Validator());
            var writer = fixture.Create<DataOwnerWriter>();

            storageDataOwner.ServiceTree = currentServiceTree;

            updatedServiceTree.ServiceId = Guid.NewGuid().ToString();
            dataOwner.ServiceTree = updatedServiceTree;

            // Required for update actions.
            storageDataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("serviceTree.serviceId", exn.Target);
            Assert.Equal(updatedServiceTree.ServiceId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree and service group id changed, then fail."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_And_ServiceGroupIdIsChanged_Then_Fail(
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            [Frozen] DataOwner storageDataOwner,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree,
            IFixture fixture)
        {
            fixture.Inject<IValidator>(new Validator());
            var writer = fixture.Create<DataOwnerWriter>();

            storageDataOwner.ServiceTree = currentServiceTree;

            updatedServiceTree.ServiceGroupId = Guid.NewGuid().ToString();
            dataOwner.ServiceTree = updatedServiceTree;

            // Required for update actions.
            storageDataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("serviceTree.serviceGroupId", exn.Target);
            Assert.Equal(updatedServiceTree.ServiceGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree and team group id changed, then fail."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_And_TeamGroupIdIsChanged_Then_Fail(
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            [Frozen] DataOwner storageDataOwner,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree,
            IFixture fixture)
        {
            fixture.Inject<IValidator>(new Validator());
            var writer = fixture.Create<DataOwnerWriter>();

            storageDataOwner.ServiceTree = currentServiceTree;

            updatedServiceTree.TeamGroupId = Guid.NewGuid().ToString();
            dataOwner.ServiceTree = updatedServiceTree;

            // Required for update actions.
            storageDataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("serviceTree.teamGroupId", exn.Target);
            Assert.Equal(updatedServiceTree.TeamGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When data owner UpdateAsync is called with service tree, then ensure all properties are immutable except writeSecurityGroups."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithServiceTree_Then_EnsureImmutableExceptWriteSecurityGroups(
            [Frozen] Mock<IValidator> validator,
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            ServiceTree currentServiceTree,
            ServiceTree updatedServiceTree)
        {
            dataOwner.ServiceTree = updatedServiceTree;
            storageDataOwner.ServiceTree = currentServiceTree;

            // Required for update actions.
            storageDataOwner.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            validator.Verify(m => m.Immutable(storageDataOwner, dataOwner, Validator.InvalidProperty, nameof(DataOwner.ServiceTree), nameof(TrackingDetails), nameof(DataOwner.WriteSecurityGroups), nameof(DataOwner.TagSecurityGroups), nameof(dataOwner.TagApplicationIds), nameof(DataOwner.SharingRequestContacts), nameof(DataOwner.Icm), nameof(DataOwner.HasInitiatedTransferRequests), nameof(DataOwner.HasPendingTransferRequests)), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with tag security groups that do not exist, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWitInvalidTagSecurityGroups_Then_Fail(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), dataOwner.TagSecurityGroups.Last())).ReturnsAsync(false);

            await Assert.ThrowsAsync<SecurityGroupNotFoundException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with tag security groups that do exist, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithValidTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            storageDataOwner.TrackingDetails = trackingDetails;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(true);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called without tag security groups, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithoutTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.TagSecurityGroups = new Guid[0];
            storageDataOwner.TrackingDetails = trackingDetails;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with null tag security groups, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithNullTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.TagSecurityGroups = null;
            storageDataOwner.TrackingDetails = trackingDetails;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with null existing tag security groups, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithNullExistingTagSecurityGroups_Then_Pass(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.TagSecurityGroups = null;
            storageDataOwner.TagSecurityGroups = null;
            storageDataOwner.TrackingDetails = trackingDetails;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with null sharing request contacts when previous set, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithNullSharingRequestContactsAndPreviouslySet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.SharingRequestContacts = null;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called with null sharing request contacts and not previously set, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithNullSharingRequestContactsAndNotPreviouslySet_Then_Pass(
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.SharingRequestContacts = null;
            storageDataOwner.SharingRequestContacts = null;
            storageDataOwner.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with empty sharing request contacts when previous set, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithEmptySharingRequestContactsAndPreviouslySet_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called with empty sharing request contacts and not previously set, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithEmptySharingRequestContactsAndNotPreviouslySet_Then_Pass(
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];
            storageDataOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];
            storageDataOwner.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with identical security groups, then skip the check."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithIdenticalTagSecurityGroups_Then_SkipCheck(
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            dataOwner.TagSecurityGroups = storageDataOwner.TagSecurityGroups;
            storageDataOwner.TrackingDetails = trackingDetails;

            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(false);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with existing service tree and no write security groups, then fail if user is not a service tree admin."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeAndNoWriteSGs_AndUserNotInAdminList_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer)
        {
            authPrincipal.UserAlias = "non-admin";

            storageDataOwner.WriteSecurityGroups = null;
            storageDataOwner.ServiceTree.ServiceAdmins = new[] { "admin" };
            dataOwner.ServiceTree = storageDataOwner.ServiceTree;

            var exn = await Assert.ThrowsAsync<ServiceTreeMissingWritePermissionException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("non-admin", exn.UserName);
            Assert.Equal($"{dataOwner.ServiceTree.ServiceId}", exn.ServiceId);
        }

        [Theory(DisplayName = "When the user is not in the team's write security grous, but in the service admin list, then succeed."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UserNotInTeamWriteSGs_But_InServiceAdminList_Then_Succeed(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            IEnumerable<Guid> writeSecurityGroups,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            authPrincipal.UserAlias = "admin";

            storageDataOwner.WriteSecurityGroups = writeSecurityGroups.ToList();
            storageDataOwner.ServiceTree.ServiceAdmins = new[] { "admin" };
            storageDataOwner.TrackingDetails = trackingDetails;
            dataOwner.ServiceTree = storageDataOwner.ServiceTree;

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree Service link, then refresh the data owner properties accordingly."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeServiceLink_Then_RefreshTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult,
            TrackingDetails trackingDetails)
        {
            var serviceTree = new Fixture().Create<ServiceTree>();
            var serviceId = Guid.NewGuid();
            serviceTree.ServiceId = serviceId.ToString();
            serviceTree.ServiceAdmins = new[] { "SomeContact1", "SomeContact2" };
            dataOwner.ServiceTree = serviceTree;
            storageDataOwner.ServiceTree = serviceTree;
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            var service = new Service
            {
                Id = serviceId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                ServiceGroupId = Guid.NewGuid(),
                ServiceGroupName = "sg name",
                TeamGroupId = Guid.NewGuid(),
                TeamGroupName = "tg name",
                Level = Client.ServiceTree.ServiceTreeLevel.Service
            };

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(service.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(service.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(service.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(service.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(service.ServiceGroupId.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(service.ServiceGroupName, result.ServiceTree.ServiceGroupName);
            Assert.Equal(service.TeamGroupId.ToString(), result.ServiceTree.TeamGroupId);
            Assert.Equal(service.TeamGroupName, result.ServiceTree.TeamGroupName);
            Assert.Equal(service.Id.ToString(), result.ServiceTree.ServiceId);
            Assert.Equal(service.Name, result.ServiceTree.ServiceName);
            Assert.Equal(service.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(service.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree TeamGroup link, then refresh the data owner properties accordingly."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeTeamGroupLink_Then_RefreshTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult,
            TrackingDetails trackingDetails)
        {
            var teamGroupId = Guid.NewGuid();
            var serviceTree = new Fixture().Create<ServiceTree>();
            serviceTree.ServiceAdmins = new[] { "SomeContact1", "SomeContact2" };
            serviceTree.TeamGroupId = teamGroupId.ToString();
            serviceTree.ServiceId = null;
            dataOwner.ServiceTree = serviceTree;
            storageDataOwner.ServiceTree = serviceTree;
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            var teamGroup = new TeamGroup
            {
                Id = teamGroupId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                ServiceGroupId = Guid.NewGuid(),
                ServiceGroupName = "sg name",
                Level = Client.ServiceTree.ServiceTreeLevel.TeamGroup
            };

            serviceClient.Setup(m => m.ReadTeamGroupWithExtendedProperties(teamGroupId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<TeamGroup>(httpResult, teamGroup));

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("(TG) Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(teamGroup.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(teamGroup.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(teamGroup.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(teamGroup.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(teamGroup.ServiceGroupId.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(teamGroup.ServiceGroupName, result.ServiceTree.ServiceGroupName);
            Assert.Equal(teamGroup.Id.ToString(), result.ServiceTree.TeamGroupId);
            Assert.Equal(teamGroup.Name, result.ServiceTree.TeamGroupName);
            Assert.Null(result.ServiceTree.ServiceId);
            Assert.Null(result.ServiceTree.ServiceName);
            Assert.Equal(teamGroup.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(teamGroup.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree ServiceGroup link, then refresh the data owner properties accordingly."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeServiceGroupLink_Then_RefreshTheDatOwnerPropertiesAccordingly(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            HttpResult httpResult,
            TrackingDetails trackingDetails)
        {
            var serviceGroupId = Guid.NewGuid();
            var serviceTree = new Fixture().Create<ServiceTree>();
            serviceTree.ServiceGroupId = serviceGroupId.ToString();
            serviceTree.ServiceAdmins = new[] { "SomeContact1", "SomeContact2" };
            serviceTree.TeamGroupId = null;
            serviceTree.ServiceId = null;
            dataOwner.ServiceTree = serviceTree;
            storageDataOwner.ServiceTree = serviceTree;
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            var serviceGroup = new ServiceGroup
            {
                Id = serviceGroupId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                Level = Client.ServiceTree.ServiceTreeLevel.ServiceGroup
            };

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(serviceGroupId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<ServiceGroup>(httpResult, serviceGroup));

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("(SG) Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(serviceGroup.OrganizationId.ToString(), result.ServiceTree.OrganizationId);
            Assert.Equal(serviceGroup.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(serviceGroup.DivisionId.ToString(), result.ServiceTree.DivisionId);
            Assert.Equal(serviceGroup.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(serviceGroup.Id.ToString(), result.ServiceTree.ServiceGroupId);
            Assert.Equal(serviceGroup.Name, result.ServiceTree.ServiceGroupName);
            Assert.Null(result.ServiceTree.TeamGroupId);
            Assert.Null(result.ServiceTree.TeamGroupName);
            Assert.Null(result.ServiceTree.ServiceId);
            Assert.Null(result.ServiceTree.ServiceName);
            Assert.Equal(serviceGroup.Level.ToString(), result.ServiceTree.Level.ToString());
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(serviceGroup.AdminUserNames, x => x);
        }

        [Theory(DisplayName = "When UpdateAsync is called with ServiceTree ServiceGroup link and the service tree call fails, then use the old data."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeServiceGroupLinkWithFail_Then_DoNotFail(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            var serviceGroupId = Guid.NewGuid();
            var serviceTree = new Fixture().Create<ServiceTree>();
            serviceTree.ServiceGroupId = serviceGroupId.ToString();
            serviceTree.TeamGroupId = null;
            serviceTree.ServiceId = null;
            serviceTree.ServiceAdmins = new[] { "admin" };
            dataOwner.ServiceTree = serviceTree;
            storageDataOwner.ServiceTree = serviceTree;
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            var serviceGroup = new ServiceGroup
            {
                Id = serviceGroupId,
                Name = "Name",
                Description = "Description",
                AdminUserNames = new[] { "admin", "admin1" },
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "orgName",
                DivisionId = Guid.NewGuid(),
                DivisionName = "divName",
                Level = Client.ServiceTree.ServiceTreeLevel.ServiceGroup
            };

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(serviceGroupId, It.IsAny<RequestContext>())).ThrowsAsync(new NotFoundError(serviceGroupId));

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            result.ServiceTree.ServiceAdmins.SortedSequenceLike(serviceTree.ServiceAdmins, x => x);
        }

        [Theory(DisplayName = "When UpdateAsync is called with different service tree metadata, then ignore those values."), ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithServiceTreeUpdates_Then_IgnoreThem(
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            TrackingDetails trackingDetails,
            Fixture fixture)
        {
            fixture.FreezeMapper();
            var writer = fixture.Create<DataOwnerWriter>();

            var serviceGroupId = Guid.NewGuid();
            var serviceTree = new Fixture().Create<ServiceTree>();
            serviceTree.ServiceGroupId = serviceGroupId.ToString();
            serviceTree.TeamGroupId = null;
            serviceTree.ServiceId = null;
            serviceTree.ServiceAdmins = new[] { "admin", "admin1" };
            dataOwner.ServiceTree = serviceTree;

            var serviceTreeOld = new Fixture().Create<ServiceTree>();
            serviceTreeOld.ServiceGroupId = serviceTree.ServiceGroupId;
            serviceTreeOld.TeamGroupId = serviceTree.TeamGroupId;
            serviceTreeOld.ServiceId = serviceTree.ServiceId;
            serviceTreeOld.ServiceAdmins = new[] { "admin", "admin2" };
            storageDataOwner.ServiceTree = serviceTreeOld;
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(serviceGroupId, It.IsAny<RequestContext>())).ThrowsAsync(new NotFoundError(serviceGroupId));

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            // Ensure properties are not mapped across.
            Assert.Equal(serviceTreeOld.DivisionId, result.ServiceTree.DivisionId);
            Assert.Equal(serviceTreeOld.DivisionName, result.ServiceTree.DivisionName);
            Assert.Equal(serviceTreeOld.OrganizationId, result.ServiceTree.OrganizationId);
            Assert.Equal(serviceTreeOld.OrganizationName, result.ServiceTree.OrganizationName);
            Assert.Equal(serviceTreeOld.ServiceGroupName, result.ServiceTree.ServiceGroupName);
            Assert.Equal(serviceTreeOld.TeamGroupName, result.ServiceTree.TeamGroupName);
            Assert.Equal(serviceTreeOld.ServiceName, result.ServiceTree.ServiceName);
            Assert.Equal(serviceTreeOld.Level, result.ServiceTree.Level);

            result.ServiceTree.ServiceAdmins.SortedSequenceLike(serviceTreeOld.ServiceAdmins, x => x);
        }
        #endregion

        #region ReplaceServiceTreeIdAsync
        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the service id does not belong to any owner, then pull the data from service tree."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndIdDoesNotExist_Then_PullFromServiceTree(
            [Frozen] DataOwner dataOwner,
            FilterResult<DataOwner> filterResult,
            [Frozen] Mock<IDataOwnerReader> reader,
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWriter writer)
        {
            int initialVersion = dataOwner.TrackingDetails.Version;
            filterResult.Total = 0;

            Action<DataOwnerFilterCriteria> verify = c =>
            {
                Assert.Equal(dataOwner.ServiceTree.ServiceId, c.ServiceTree.ServiceId.Value);
                Assert.Equal(StringComparisonType.EqualsCaseSensitive, c.ServiceTree.ServiceId.ComparisonType);
            };

            Guid serviceId = Guid.Parse(dataOwner.ServiceTree.ServiceId);

            IEnumerable<string> adminGroups = new[] { "admin", "admin1" };

            var service = new Service
            {
                Name = "Name",
                Description = "Description",
                AdminUserNames = adminGroups
            };

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            reader.Setup(m => m.ReadByFiltersAsync(Is.Value(verify), ExpandOptions.ServiceTree | ExpandOptions.TrackingDetails)).ReturnsAsync(filterResult);

            var result = await writer.ReplaceServiceIdAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal("Name", result.Name);
            Assert.Equal("Description", result.Description);
            Assert.Equal(initialVersion + 1, dataOwner.TrackingDetails.Version);
            result.ServiceTree.ServiceAdmins.SortedSequenceLike(adminGroups, x => x);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the service id does not belong to any owner or an entity in service tree, then fail."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndIdDoesNotExistInServiceTree_Then_Fail(
            [Frozen] DataOwner dataOwner,
            [Frozen] FilterResult<DataOwner> filterResult,
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwnerWriter writer)
        {
            filterResult.Total = 0;
            serviceClient
                .Setup(m => m.ReadServiceWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.ServiceId), It.IsAny<RequestContext>()))
                .ThrowsAsync(new NotFoundError(Guid.Parse(dataOwner.ServiceTree.ServiceId)));

            await Assert.ThrowsAsync<ServiceNotFoundException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called a non-guid service id, then throw invalid property error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndIdMalformed_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.ServiceTree.ServiceId = "badGuid";

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("serviceTree.serviceId", exn.ParamName);
            Assert.Equal("badGuid", exn.Value);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the provided owner is not found, then throw not found error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndOwnerNotFound_Then_Fail(
            DataOwner dataOwner,
            [Frozen] Mock<IDataOwnerReader> reader,
            DataOwnerWriter writer)
        {
            reader.Setup(m => m.ReadByIdAsync(dataOwner.Id, ExpandOptions.TrackingDetails | ExpandOptions.ServiceTree)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("DataOwner", exn.EntityType);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the ETag has changed, then throw ETagMismatch error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndETagChanged_Then_Fail(
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer)
        {
            storageDataOwner.ETag = "otherEtag";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(dataOwner.ETag, exn.Value);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the user is not in the write security group of the existing owner, then throw permission error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndNotInWriteSecurityGroup_Then_Fail(
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            IFixture fixture,
            DataOwnerWriter writer)
        {
            storageDataOwner.WriteSecurityGroups = fixture.CreateMany<Guid>();

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the service id owner is linked to a data agent, then throw conflict error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndLinkedToAgent_Then_Fail(
            DataOwner serviceTreeOwner,
            FilterResult<DataAgent> agentResults,
            [Frozen] DataOwner dataOwner,
            [Frozen] FilterResult<DataOwner> searchResults,
            [Frozen] Mock<IDataAgentReader> agentReader,
            DataOwnerWriter writer)
        {
            searchResults.Total = 1;
            searchResults.Values = new[] { serviceTreeOwner };
            agentResults.Total = 1;

            Action<DataAgentFilterCriteria> verify = filter =>
            {
                Assert.Equal(serviceTreeOwner.Id, filter.OwnerId);
                Assert.Equal(0, filter.Count);
            };

            agentReader
                .Setup(m => m.ReadByFiltersAsync(Is.Value(verify), ExpandOptions.None))
                .ReturnsAsync(agentResults);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.AlreadyExists, exn.ConflictType);
            Assert.Equal(serviceTreeOwner.Id.ToString(), exn.Value);
            Assert.Equal("dataAgent.ownerId", exn.Target);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and the service id owner is linked to an asset group, then throw conflict error."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdAndLinkedToAssetGroup_Then_Fail(
            DataOwner serviceTreeOwner,
            FilterResult<AssetGroup> assetGroupResults,
            [Frozen] DataOwner dataOwner,
            [Frozen] FilterResult<DataOwner> searchResults,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            DataOwnerWriter writer)
        {
            searchResults.Total = 1;
            searchResults.Values = new[] { serviceTreeOwner };
            assetGroupResults.Total = 1;

            Action<AssetGroupFilterCriteria> verify = filter =>
            {
                Assert.Equal(serviceTreeOwner.Id, filter.OwnerId);
                Assert.Equal(0, filter.Count);
            };

            assetGroupReader
                .Setup(m => m.ReadByFiltersAsync(Is.Value(verify), ExpandOptions.None))
                .ReturnsAsync(assetGroupResults);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.AlreadyExists, exn.ConflictType);
            Assert.Equal(serviceTreeOwner.Id.ToString(), exn.Value);
            Assert.Equal("assetGroup.ownerId", exn.Target);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called, then delete existing service tree owner and update provided owner."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeId_Then_DeleteExistingOwner(
            DataOwner dataOwner,
            [Frozen] FilterResult<DataOwner> searchResults,
            [Frozen] DataOwner storageDataOwner,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwnerWriter writer)
        {
            searchResults.Total = 1;
            searchResults.Values = searchResults.Values.Take(1); // There should only be one result.

            var serviceTreeOwner = searchResults.Values.Single();
            serviceTreeOwner.Id = Guid.NewGuid();

            Action<IEnumerable<DataOwner>> updateVerify = values =>
            {
                Assert.Contains(storageDataOwner, values); // Ensure storage value is pushed back for updates.
                Assert.Contains(serviceTreeOwner, values); // Ensure service tree owner is pushed back for updates.
                Assert.True(values.Single(x => x.Id == serviceTreeOwner.Id).IsDeleted); // Ensure service tree owner is soft deleted.
            };

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(updateVerify)))
                .ReturnsAsync(new[] { storageDataOwner, serviceTreeOwner });

            var results = await writer.ReplaceServiceIdAsync(dataOwner).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(updateVerify)), Times.Once);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called, then migrate properties."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeId_Then_MigrateProperties(
            DataOwner serviceTreeOwner,
            Service service,
            HttpResult httpResult,
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            [Frozen] DataOwner dataOwner,
            [Frozen] FilterResult<DataOwner> searchResults,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwnerWriter writer)
        {
            service.AdminUserNames = new[] { "admin" }; // Ensure user is authorized.
            serviceTreeOwner.Id = Guid.NewGuid();

            searchResults.Total = 1;
            searchResults.Values = new[] { serviceTreeOwner };

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<DataOwner>>()))
                .ReturnsAsync(new[] { dataOwner, serviceTreeOwner });

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(Guid.Parse(serviceTreeOwner.ServiceTree.ServiceId), It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            var updatedOwner = await writer.ReplaceServiceIdAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(updatedOwner.Name, serviceTreeOwner.Name);
            Assert.Equal(updatedOwner.Description, serviceTreeOwner.Description);
            Assert.Null(updatedOwner.AlertContacts);
            Assert.Null(updatedOwner.AnnouncementContacts);

            Assert.Equal(updatedOwner.ServiceTree.DivisionId, service.DivisionId.ToString());
            Assert.Equal(updatedOwner.ServiceTree.OrganizationId, service.OrganizationId.ToString());
            Assert.Equal(updatedOwner.ServiceTree.ServiceAdmins, service.AdminUserNames);
            Assert.Equal(updatedOwner.ServiceTree.ServiceGroupId, service.ServiceGroupId.ToString());
            Assert.Equal(updatedOwner.ServiceTree.ServiceId, service.Id.ToString());
            Assert.Equal(updatedOwner.ServiceTree.TeamGroupId, service.TeamGroupId.ToString());

            Assert.Equal(updatedOwner.TrackingDetails.EgressedOn, serviceTreeOwner.TrackingDetails.EgressedOn);
        }

        [Theory(DisplayName = "When data owner ReplaceServiceTreeIdAsync is called and user is not in the service admins of the target service tree entry, then fail."), ValidReplaceData]
        public async Task When_ReplacingServiceTreeIdNotServiceAdmin_Then_Fail(
            DataOwner dataOwner,
            Service service,
            HttpResult httpResult,
            [Frozen] Mock<IServiceTreeClient> serviceClient,
            DataOwnerWriter writer)
        {
            service.AdminUserNames = new[] { "other" }; // Ensure user is not authorized.

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.ServiceId), It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            await Assert.ThrowsAsync<ServiceTreeMissingWritePermissionException>(() => writer.ReplaceServiceIdAsync(dataOwner)).ConfigureAwait(false);
        }
        #endregion

        #region AutoFixture Customizations
        public class ValidServiceTreeDataAttribute : ValidDataAttribute
        {
            public ValidServiceTreeDataAttribute(WriteAction action = WriteAction.Create) : base(action)
            {
                var serviceId = this.Fixture.Create<Guid>();

                this.Fixture.Customize<ServiceTree>(obj =>
                    obj
                    .With(x => x.ServiceId, serviceId.ToString())
                    .Without(x => x.ServiceAdmins)
                    .Without(x => x.DivisionId)
                    .Without(x => x.DivisionName)
                    .Without(x => x.OrganizationId)
                    .Without(x => x.OrganizationName)
                    .Without(x => x.ServiceGroupId)
                    .Without(x => x.ServiceGroupName)
                    .Without(x => x.TeamGroupId)
                    .Without(x => x.TeamGroupName)
                    .Without(x => x.ServiceName));

                this.Fixture.FreezeMapper();

                IEnumerable<MailAddress> mailAddresses = new[] { new MailAddress("foo@company.com", "foo") };
                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, this.WriteSecurityGroups)
                    .With(x => x.ServiceTree, this.Fixture.Create<ServiceTree>())
                    .With(x => x.SharingRequestContacts, mailAddresses)
                    .With(x => x.TagSecurityGroups, this.Fixture.CreateMany<Guid>().ToList())
                    .With(x => x.TagApplicationIds, this.Fixture.CreateMany<Guid>().ToList())
                    .Without(x => x.Name)
                    .Without(x => x.Description)
                    .Without(x => x.AlertContacts)
                    .Without(x => x.AnnouncementContacts));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, this.WriteSecurityGroups)
                    .With(x => x.ServiceTree, this.Fixture.Create<ServiceTree>())
                    .With(x => x.SharingRequestContacts, mailAddresses)
                    .With(x => x.TagSecurityGroups, this.Fixture.CreateMany<Guid>().ToList())
                    .With(x => x.TagApplicationIds, this.Fixture.CreateMany<Guid>().ToList())
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .Without(x => x.Name)
                    .Without(x => x.Description)
                    .Without(x => x.AlertContacts)
                    .Without(x => x.AnnouncementContacts));
                }

                var identity = new ClaimsIdentity(new Claim[0]);
                identity.BootstrapContext = new System.IdentityModel.Tokens.BootstrapContext("test");
                var claimsPrincipal = new ClaimsPrincipal(identity);

                var userName = "admin";

                this.Fixture.Customize<AuthenticatedPrincipal>(obj =>
                    obj
                    .With(x => x.ClaimsPrincipal, claimsPrincipal)
                    .With(x => x.UserAlias, userName.ToUpper()));

                var httpResult = this.Fixture.Create<HttpResult>();

                var service = this.Fixture.Create<Service>();
                service.Id = serviceId;
                service.AdminUserNames = new[] { userName };
                service.AdminSecurityGroups = userName;

                this.Fixture.Customize<Mock<IServiceTreeClient>>(entity =>
                    entity.Do(x => x.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service))));
            }
        }

        public class ValidReplaceDataAttribute : AutoMoqDataAttribute
        {
            public ValidReplaceDataAttribute() : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>().ToList();

                this.Fixture.FreezeMapper();

                var serviceId = Guid.NewGuid();

                this.Fixture.Customize<ServiceTree>(obj =>
                    obj
                    .With(x => x.ServiceId, serviceId.ToString())
                    .Without(x => x.ServiceAdmins)
                    .Without(x => x.DivisionId)
                    .Without(x => x.DivisionName)
                    .Without(x => x.OrganizationId)
                    .Without(x => x.OrganizationName)
                    .Without(x => x.ServiceGroupId)
                    .Without(x => x.ServiceGroupName)
                    .Without(x => x.TeamGroupId)
                    .Without(x => x.TeamGroupName)
                    .Without(x => x.ServiceName));

                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .With(x => x.ETag, "ETag")
                    .With(x => x.Id, Guid.NewGuid())
                    .Without(x => x.DataAgents)
                    .Without(x => x.AssetGroups));

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups);

                // Ensure no failures due to existing items.
                this.Fixture.Customize<FilterResult<DataAgent>>(obj =>
                    obj.With(x => x.Total, 0));

                this.Fixture.Customize<FilterResult<AssetGroup>>(obj =>
                    obj.With(x => x.Total, 0));

                var identity = new ClaimsIdentity(new Claim[0]);
                identity.BootstrapContext = new System.IdentityModel.Tokens.BootstrapContext("test");
                var claimsPrincipal = new ClaimsPrincipal(identity);

                var userName = "admin";

                var httpResult = this.Fixture.Create<HttpResult>();

                var service = this.Fixture.Create<Service>();
                service.Id = serviceId;
                service.AdminUserNames = new[] { userName };
                service.AdminSecurityGroups = userName;

                this.Fixture.Customize<Mock<IServiceTreeClient>>(entity =>
                    entity.Do(x => x.Setup(m => m.ReadServiceWithExtendedProperties(serviceId, It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service))));

                this.Fixture.Customize<AuthenticatedPrincipal>(obj =>
                    obj
                    .With(x => x.ClaimsPrincipal, claimsPrincipal)
                    .With(x => x.UserAlias, userName));
            }
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create) : base(true)
            {
                this.WriteSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>().ToList();

                this.Fixture.Customize<Icm>(obj =>
                    obj
                    .Without(x => x.Source)
                    .Without(x => x.TenantId));

                this.Fixture.Customize<Entity>(obj =>
                    obj
                    .Without(x => x.Id)
                    .Without(x => x.ETag)
                    .Without(x => x.TrackingDetails));

                this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.QualifierParts, this.Fixture.Create<AssetQualifier>().Properties));

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, this.WriteSecurityGroups)
                    .Without(x => x.DataAgents)
                    .Without(x => x.AssetGroups)
                    .Without(x => x.ServiceTree)
                    .Without(x => x.HasInitiatedTransferRequests)
                    .Without(x => x.HasPendingTransferRequests));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, this.WriteSecurityGroups)
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .Without(x => x.DataAgents)
                    .Without(x => x.AssetGroups)
                    .Without(x => x.ServiceTree)
                    .Without(x => x.HasInitiatedTransferRequests)
                    .Without(x => x.HasPendingTransferRequests));
                }

                this.Fixture.Customize<FilterResult<DataOwner>>(obj =>
                    obj
                    .With(x => x.Values, Enumerable.Empty<DataOwner>()));

                this.Fixture.RegisterAuthorizationClasses(this.WriteSecurityGroups);
            }

            public IEnumerable<Guid> WriteSecurityGroups { get; set; }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
        #endregion
    }
}