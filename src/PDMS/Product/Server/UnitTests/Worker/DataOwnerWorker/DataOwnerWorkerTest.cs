namespace Microsoft.PrivacyServices.DataManagement.Worker.DataOwner.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Worker.DataOwner;
    using Microsoft.PrivacyServices.DataManagement.Worker.DataOwnerWorker;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    using ST = Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

    public class DataOwnerWorkerTest
    {
        [Theory(DisplayName = "Verify fallback behavior for service level mismatch."), ValidData]
        public async Task VerifyServiceLevelFallback(
            Lock<DataOwnerWorkerLockState> lockStatus,
            DataOwner dataOwner,
            ST.Service actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = (ServiceTreeLevel)10;

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(new HttpResult<ST.Service>(httpResult, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(It.IsAny<DataOwner>()), Times.Never);
        }

        [Theory(Skip = "Do we need to keep null or empty strings in DB?", DisplayName = "When the data owner has the service id of an existing entity, then update the fields.")]
        [InlineValidData("")]
        [InlineValidData(null)]
        public async Task VerifyDataRefresh(
            string initialValue,
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.Service actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.Service;
            dataOwner.ServiceTree.ServiceId = actualServiceTreeValue.Id.ToString();
            actualServiceTreeValue.Description = initialValue;
            actualServiceTreeValue.AdminUserNames = new List<string> { initialValue };
            actualServiceTreeValue.DivisionId = string.IsNullOrEmpty(initialValue) ? (Guid?)null : Guid.NewGuid();
            actualServiceTreeValue.DivisionName = initialValue;
            actualServiceTreeValue.OrganizationId = string.IsNullOrEmpty(initialValue) ? (Guid?)null : Guid.NewGuid();
            actualServiceTreeValue.OrganizationName = initialValue;
            actualServiceTreeValue.ServiceGroupId = string.IsNullOrEmpty(initialValue) ? (Guid?)null : Guid.NewGuid();
            actualServiceTreeValue.ServiceGroupName = initialValue;
            actualServiceTreeValue.TeamGroupId = string.IsNullOrEmpty(initialValue) ? (Guid?)null : Guid.NewGuid();
            actualServiceTreeValue.TeamGroupName = initialValue;
            actualServiceTreeValue.Name = initialValue;

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(new HttpResult<ST.Service>(httpResult, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.Equal(actualServiceTreeValue.Id.ToString(), x.ServiceTree.ServiceId);
                Assert.Equal(dataOwner.Description, x.Description);
                Assert.True(this.ContainSameItems<string>(dataOwner.ServiceTree.ServiceAdmins, x.ServiceTree.ServiceAdmins));
                Assert.Equal(dataOwner.Name, x.Name);
                Assert.Null(x.ServiceTree.DivisionId);
                Assert.Equal(x.ServiceTree.DivisionName, initialValue);
                Assert.Null(x.ServiceTree.OrganizationId);
                Assert.Equal(x.ServiceTree.OrganizationName, initialValue);
                Assert.Null(x.ServiceTree.ServiceGroupId);
                Assert.Equal(x.ServiceTree.ServiceGroupName, initialValue);
                Assert.Null(x.ServiceTree.TeamGroupId);
                Assert.Equal(x.ServiceTree.TeamGroupName, initialValue);
                Assert.Equal(x.ServiceTree.ServiceName, initialValue);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "Verify data mapping for service group."), ValidData]
        public async Task VerifyDataMappingServiceGroup(
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.ServiceGroup actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.ServiceGroup;
            dataOwner.ServiceTree.ServiceGroupId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(new HttpResult<ST.ServiceGroup>(httpResult, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.NotEqual(actualServiceTreeValue.Id.ToString(), x.ServiceTree.ServiceId);
                Assert.NotEqual(actualServiceTreeValue.Name, x.ServiceTree.ServiceName);

                Assert.Equal("(SG) " + actualServiceTreeValue.Name, x.Name);

                Assert.Equal(actualServiceTreeValue.Description, x.Description);
                Assert.True(this.ContainSameItems<string>(actualServiceTreeValue.AdminUserNames, x.ServiceTree.ServiceAdmins));
                Assert.Equal(actualServiceTreeValue.DivisionId.ToString(), x.ServiceTree.DivisionId);
                Assert.Equal(actualServiceTreeValue.DivisionName, x.ServiceTree.DivisionName);
                Assert.Equal(actualServiceTreeValue.OrganizationId.ToString(), x.ServiceTree.OrganizationId);
                Assert.Equal(actualServiceTreeValue.OrganizationName, x.ServiceTree.OrganizationName);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "Verify Pagination logic, Total returned rows = 0"), ValidData]
        public async Task VerifyPagination(
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.ServiceGroup actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.ServiceGroup;
            dataOwner.ServiceTree.ServiceGroupId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(Enumerable.Repeat(dataOwner, 0).ToList()));

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(new HttpResult<ST.ServiceGroup>(httpResult, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            storageReader.Verify(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        }

        [Theory(Skip = "For checking TaskCanceledException, skipping it, since retries and delay added in minutes would increase the overall time for UTs",
            DisplayName = "Verify Task canceled exception"), ValidData]
        public async Task VerifyTaskCanceledException(
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.ServiceGroup actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.ServiceGroup;
            dataOwner.ServiceTree.ServiceGroupId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(Enumerable.Repeat(dataOwner, 1).ToList()));

            serviceClient.Setup(m => m.ReadServiceGroupWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .Throws(new TaskCanceledException());

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            storageReader.Verify(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);

            // verify max retry by calling serviceTree 5 times.
            serviceClient.Verify(m => m.ReadServiceGroupWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()), Times.Exactly(5));
        }


        [Theory(DisplayName = "Verify data mapping for team group."), ValidData]
        public async Task VerifyDataMappingTeamGroup(
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.TeamGroup actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult result,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.TeamGroup;
            dataOwner.ServiceTree.TeamGroupId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceClient
                .Setup(m => m.ReadTeamGroupWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.TeamGroupId), It.IsAny<DataManagement.Client.RequestContext>()))
                .ReturnsAsync(new DataManagement.Client.HttpResult<ST.TeamGroup>(result, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.NotEqual(actualServiceTreeValue.Id.ToString(), x.ServiceTree.ServiceId);
                Assert.NotEqual(actualServiceTreeValue.Name, x.ServiceTree.ServiceName);

                Assert.Equal(actualServiceTreeValue.Id.ToString(), x.ServiceTree.TeamGroupId);
                Assert.Equal(actualServiceTreeValue.Name, x.ServiceTree.TeamGroupName);
                Assert.Equal(actualServiceTreeValue.ServiceGroupId.ToString(), x.ServiceTree.ServiceGroupId);
                Assert.Equal(actualServiceTreeValue.ServiceGroupName, x.ServiceTree.ServiceGroupName);

                Assert.Equal("(TG) " + actualServiceTreeValue.Name, x.Name);

                Assert.Equal(actualServiceTreeValue.Description, x.Description);
                Assert.True(this.ContainSameItems<string>(actualServiceTreeValue.AdminUserNames, x.ServiceTree.ServiceAdmins));
                Assert.Equal(actualServiceTreeValue.DivisionId.ToString(), x.ServiceTree.DivisionId);
                Assert.Equal(actualServiceTreeValue.DivisionName, x.ServiceTree.DivisionName);
                Assert.Equal(actualServiceTreeValue.OrganizationId.ToString(), x.ServiceTree.OrganizationId);
                Assert.Equal(actualServiceTreeValue.OrganizationName, x.ServiceTree.OrganizationName);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "Verify data mapping for service."), ValidData]
        public async Task VerifyDataMappingService(
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            ST.Service actualServiceTreeValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceClient,
            HttpResult httpResult,
            DataOwnerWorker worker)
        {
            dataOwner.ServiceTree.Level = ServiceTreeLevel.Service;
            dataOwner.ServiceTree.ServiceId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceClient.Setup(m => m.ReadServiceWithExtendedProperties(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(new HttpResult<ST.Service>(httpResult, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.Equal(actualServiceTreeValue.Id.ToString(), x.ServiceTree.ServiceId);
                Assert.Equal(actualServiceTreeValue.Name, x.ServiceTree.ServiceName);
                Assert.Equal(actualServiceTreeValue.TeamGroupId.ToString(), x.ServiceTree.TeamGroupId);
                Assert.Equal(actualServiceTreeValue.TeamGroupName, x.ServiceTree.TeamGroupName);
                Assert.Equal(actualServiceTreeValue.ServiceGroupId.ToString(), x.ServiceTree.ServiceGroupId);
                Assert.Equal(actualServiceTreeValue.ServiceGroupName, x.ServiceTree.ServiceGroupName);

                Assert.Equal(actualServiceTreeValue.Name, x.Name);

                Assert.Equal(actualServiceTreeValue.Description, x.Description);
                Assert.True(this.ContainSameItems<string>(actualServiceTreeValue.AdminUserNames, x.ServiceTree.ServiceAdmins));
                Assert.Equal(actualServiceTreeValue.DivisionId.ToString(), x.ServiceTree.DivisionId);
                Assert.Equal(actualServiceTreeValue.DivisionName, x.ServiceTree.DivisionName);
                Assert.Equal(actualServiceTreeValue.OrganizationId.ToString(), x.ServiceTree.OrganizationId);
                Assert.Equal(actualServiceTreeValue.OrganizationName, x.ServiceTree.OrganizationName);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "Verify service tree refresh for service group.")]
        [InlineValidData((string)null)]
        [InlineValidData("")]
        [InlineValidData("00000000-0000-0000-0000-000000000000")]
        public async Task VerifyServiceGroupRefresh(
            string orphanedDivisionId,
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            ST.ServiceGroup actualServiceTreeValue,
            DataManagement.Client.IHttpResult result,
            [Frozen] Mock<ST.IServiceTreeClient> serviceTreeClient,
            DataOwnerWorker worker)
        {
            actualServiceTreeValue.DivisionId = orphanedDivisionId == null ? (Guid?)null : Guid.Empty; ;
            dataOwner.ServiceTree.Level = ServiceTreeLevel.ServiceGroup;
            dataOwner.ServiceTree.ServiceGroupId = actualServiceTreeValue.Id.ToString();

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceTreeClient
                .Setup(m => m.ReadServiceGroupWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.ServiceGroupId), It.IsAny<DataManagement.Client.RequestContext>()))
                .ReturnsAsync(new DataManagement.Client.HttpResult<ST.ServiceGroup>(result, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.Null(x.ServiceTree.ServiceId);
                Assert.Null(x.ServiceTree.ServiceName);
                Assert.Null(x.ServiceTree.TeamGroupId);
                Assert.Null(x.ServiceTree.TeamGroupName);
                Assert.Equal(actualServiceTreeValue.Id.ToString(), x.ServiceTree.ServiceGroupId);
                Assert.Equal(actualServiceTreeValue.Name, x.ServiceTree.ServiceGroupName);

                Assert.Equal("(SG) " + actualServiceTreeValue.Name, x.Name);

                Assert.Equal(actualServiceTreeValue.Description, x.Description);
                Assert.Equal(actualServiceTreeValue.AdminUserNames, x.ServiceTree.ServiceAdmins);
                Assert.Equal(actualServiceTreeValue.DivisionId?.ToString(), x.ServiceTree.DivisionId);
                Assert.Equal(actualServiceTreeValue.DivisionName, x.ServiceTree.DivisionName);
                Assert.Equal(actualServiceTreeValue.OrganizationId?.ToString(), x.ServiceTree.OrganizationId);
                Assert.Equal(actualServiceTreeValue.OrganizationName, x.ServiceTree.OrganizationName);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "Verify service tree refresh for team group.")]
        [InlineValidData((string)null)]
        [InlineValidData("")]
        [InlineValidData("00000000-0000-0000-0000-000000000000")]
        public async Task VerifyTeamGroupRefresh(
            string orphanedDivisionId,
            Lock<DataOwnerWorkerLockState> lockStatus,
            [Frozen] DataOwner dataOwner,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            ST.TeamGroup actualServiceTreeValue,
            DataManagement.Client.IHttpResult result,
            [Frozen] Mock<ST.IServiceTreeClient> serviceTreeClient,
            DataOwnerWorker worker)
        {
            actualServiceTreeValue.DivisionId = orphanedDivisionId == null ? (Guid?)null : Guid.Empty;
            dataOwner.ServiceTree.Level = ServiceTreeLevel.TeamGroup;
            dataOwner.ServiceTree.TeamGroupId = actualServiceTreeValue.Id.ToString();
            dataOwner.IsDeleted = false;

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceTreeClient
                .Setup(m => m.ReadTeamGroupWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.TeamGroupId), It.IsAny<DataManagement.Client.RequestContext>()))
                .ReturnsAsync(new DataManagement.Client.HttpResult<ST.TeamGroup>(result, actualServiceTreeValue));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            Action<DataOwner> verify = x =>
            {
                Assert.Null(x.ServiceTree.ServiceId);
                Assert.Null(x.ServiceTree.ServiceName);
                Assert.Equal(actualServiceTreeValue.Id.ToString(), x.ServiceTree.TeamGroupId);
                Assert.Equal(actualServiceTreeValue.Name, x.ServiceTree.TeamGroupName);
                Assert.Equal(actualServiceTreeValue.ServiceGroupId.ToString(), x.ServiceTree.ServiceGroupId);
                Assert.Equal(actualServiceTreeValue.ServiceGroupName, x.ServiceTree.ServiceGroupName);

                Assert.Equal("(TG) " + actualServiceTreeValue.Name, x.Name);

                Assert.Equal(actualServiceTreeValue.Description, x.Description);
                Assert.Equal(actualServiceTreeValue.AdminUserNames, x.ServiceTree.ServiceAdmins);
                Assert.Equal(actualServiceTreeValue.DivisionId?.ToString(), x.ServiceTree.DivisionId);
                Assert.Equal(actualServiceTreeValue.DivisionName, x.ServiceTree.DivisionName);
                Assert.Equal(actualServiceTreeValue.OrganizationId?.ToString(), x.ServiceTree.OrganizationId);
                Assert.Equal(actualServiceTreeValue.OrganizationName, x.ServiceTree.OrganizationName);
            };

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When the service tree entry is not found, then do nothing.")]
        [InlineValidData((string)null)]
        [InlineValidData("")]
        [InlineValidData("00000000-0000-0000-0000-000000000000")]
        public async Task VerifyServiceTreeEntryNotFound(
            string orphanedDivisionId,
            Lock<DataOwnerWorkerLockState> lockStatus,
            ST.TeamGroup actualServiceTreeValue,
            [Frozen] DataOwner dataOwner,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ST.IServiceTreeClient> serviceTreeClient,
            DataOwnerWorker worker)
        {
            actualServiceTreeValue.DivisionId = orphanedDivisionId == null ? (Guid?)null : Guid.Empty;
            dataOwner.ServiceTree.Level = ServiceTreeLevel.TeamGroup;
            dataOwner.ServiceTree.TeamGroupId = actualServiceTreeValue.Id.ToString();
            dataOwner.IsDeleted = false;

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult(new List<DataOwner> { dataOwner }));

            serviceTreeClient
                .Setup(m => m.ReadTeamGroupWithExtendedProperties(Guid.Parse(dataOwner.ServiceTree.TeamGroupId), It.IsAny<DataManagement.Client.RequestContext>()))
                .ThrowsAsync(new ST.NotFoundError(Guid.Empty));

            await worker.DoLockWorkAsync(lockStatus, CancellationToken.None).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(It.IsAny<DataOwner>()), Times.Never);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customizations.Add(new IdSpecimenBuilder());

                this.Fixture.Customize<DataOwnerWorkerLockState>(x => x.With(y => y.InProgress, true));
                this.Fixture.Customize<DataOwner>(x =>
                    x
                    .Without(y => y.DataAgents)
                    .Without(y => y.AssetGroups));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }

        private FilterResult<DataOwner> CreateDataOwnersFilterResult(List<DataOwner> dataOwners)
        {
            return new FilterResult<DataOwner>
            {
                Values = dataOwners,
                Index = 0,
                Count = dataOwners.Count,
                Total = dataOwners.Count
            };
        }

        private bool ContainSameItems<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first.Count() != second.Count())
            {
                return false;
            }

            var firstNotSecond = first.Except(second).ToList();
            var secondNotFirst = second.Except(first).ToList();

            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }
    }
}