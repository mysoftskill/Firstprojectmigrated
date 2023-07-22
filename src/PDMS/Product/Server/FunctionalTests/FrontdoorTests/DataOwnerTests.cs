namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class DataOwnerTests : TestBase
    {
        /*
         * >V2.DataOwners.FindByAuthenticatedUser
         */
        [TestMethod]
        public async Task WhenICallApiToReadAllDataOwnersIGetNonZeroResultsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync("api/v2/DataOwners").ConfigureAwait(false);
            Assert.IsNotNull(content);

            var dataOwners = JsonConvert.DeserializeObject<ODataResponse<List<DataOwner>>>(content);
            Assert.IsTrue(dataOwners.Value.Count > 0);

            await CleanupDataOwner(owner);
        }

        [TestMethod]
        public async Task WhenICreateADataOwnerWithoutServiceTreeAndReadItSucceedsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var dataOwnerId = await GetADataOwnerIdAsync(owner.Id).ConfigureAwait(false);
            Assert.AreEqual(owner.Id, dataOwnerId);

            await CleanupDataOwner(owner);
        }

        [TestMethod]
        public async Task WhenIReadADataOwnerByNameItSucceedsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var response = await TestSetup.PdmsClientInstance.DataOwners.ReadByFiltersAsync(
                    TestSetup.RequestContext,
                    DataOwnerExpandOptions.None,
                    new DataOwnerFilterCriteria
                    {
                        Count = 1,
                        Index = 0,
                        Name = new StringFilter(owner.Name, StringComparisonType.Equals)
                    })
                .ConfigureAwait(false);

            Assert.AreEqual(response.HttpStatusCode, HttpStatusCode.OK);
            Assert.AreEqual(owner.Id, response?.Response?.Value?.FirstOrDefault()?.Id);

            await CleanupDataOwner(owner);
        }

        [TestMethod]
        public async Task WhenICreateADataOwnerWithoutServiceTreeAndDeleteItSucceedsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var response = await TestSetup.PdmsClientInstance.DataOwners.DeleteAsync(
                    owner.Id,
                    owner.ETag,
                    TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.AreEqual(response.HttpStatusCode, HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task WhenIUpdateADataOwnerItSucceedsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            owner.Description = "new Description";
            owner.Name = Guid.NewGuid().ToString();
            owner.WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                };
            owner.AlertContacts = new List<string>()
                {
                    "adgpdmsauditdri@microsoft.com"
                };
            owner.AnnouncementContacts = new List<string>()
                {
                    "ngpcieng@microsoft.com"
                };

            var response = await TestSetup.PdmsClientInstance.DataOwners.UpdateAsync(
                    owner,
                    TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.AreEqual(response.HttpStatusCode, HttpStatusCode.OK);

            Assert.AreEqual(response.Response.Id, owner.Id);
            Assert.AreEqual(response.Response.Description, owner.Description);
            Assert.AreEqual(response.Response.Name, owner.Name);
            Assert.AreEqual(response.Response.WriteSecurityGroups.First(), owner.WriteSecurityGroups.First());
            Assert.AreEqual(response.Response.AlertContacts.First(), owner.AlertContacts.First());
            Assert.AreEqual(response.Response.AnnouncementContacts.First(), owner.AnnouncementContacts.First());

            await CleanupDataOwner(response.Response);
        }

        [TestMethod]
        public async Task WhenIReadADataOwnerByIdItSucceedsAsync()
        {
            var dataOwnerId = await GetADataOwnerIdAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync($"api/v2/DataOwners/{dataOwnerId}").ConfigureAwait(false);
            Assert.IsNotNull(content);

            var dataOwner = JsonConvert.DeserializeObject<DataOwner>(content);
            Assert.IsNotNull(dataOwner);
            Assert.AreEqual(dataOwner.Id, dataOwnerId);
        }

        [TestMethod]
        public async Task WhenICreateADataOwnerWithServiceTreeItSucceedsAsync()
        {
            var dataOwner = new DataOwner
            {
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "134fd358-9776-4dc3-bcfd-26eaa1729cfc" // MEE Privacy Service FCT (in ST-PPE)
                }
            };

            var agResponse = await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);

            var newDataOwner = agResponse.Response;
            Assert.AreEqual("134fd358-9776-4dc3-bcfd-26eaa1729cfc", newDataOwner.ServiceTree.ServiceId);

            // Must make sure to remove it or the next run will fail
            await CleanupDataOwner(newDataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument))]
        public async Task WhenICallCreateDataOwnersWithNullDataOwnerItFailsAsync()
        {
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(null, TestSetup.RequestContext)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument.MutuallyExclusive))]
        public async Task WhenICreateADataOwnerWithServiceTreeIdAndAlertContactsItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                AlertContacts = new List<string>()
                {
                    "ngpcieng@microsoft.com"
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac"
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument))]
        public async Task WhenICreateADataOwnerWithServiceTreeIdAndTeamGroupIdItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac",
                    TeamGroupId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac"
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument))]
        public async Task WhenICreateADataOwnerWithServiceTreeIdAndServiceGroupIdItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac",
                    ServiceGroupId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac"
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument.MutuallyExclusive))]
        public async Task WhenICreateADataOwnerWithNameAndServiceTreeIdItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                Name = "name",
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac"
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument.MutuallyExclusive))]
        public async Task WhenICreateADataOwnerWithDescAndServiceTreeIdItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                Description = "name",
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                },
                ServiceTree = new ServiceTree
                {
                    ServiceId = "3d65f7a6-4a9c-47dc-9af1-8cd555d1fbac"
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.NullArgument))]
        public async Task WhenICreateADataOwnerWithoutANameItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                Name = "name",
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.NullArgument))]
        public async Task WhenICreateADataOwnerWithoutADescItFailsAsync()
        {
            var dataOwner = new DataOwner
            {
                Description = "desc",
                WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                }
            };
            await TestSetup.PdmsClientInstance.DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            await CleanupDataOwner(dataOwner);
        }

        [TestMethod]
        [ExpectedException(typeof(ConflictError.AlreadyExists))]
        public async Task WhenIUpdateADataOwnerNameShouldBeUniqueAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var secondOwner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            try
            {
                owner.Name = secondOwner.Name;
                var ownerUpdateResponse = await TestSetup.PdmsClientInstance.DataOwners.UpdateAsync(
                        owner,
                        TestSetup.RequestContext)
                    .ConfigureAwait(false);
            }
            finally
            {
                await CleanupDataOwner(owner);
                await CleanupDataOwner(secondOwner);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConflictError.LinkedEntityExists))]
        public async Task WhenIDeleteADataOwnerWithAgentItFailsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var agent = await CreateNewDataAgentAsync(owner.Id).ConfigureAwait(false);
            try
            {
                var ownerDeleteResponse = await TestSetup.PdmsClientInstance.DataOwners.DeleteAsync(
                        owner.Id,
                        owner.ETag,
                        TestSetup.RequestContext)
                    .ConfigureAwait(false);
            }
            finally
            {
                await CleanupDataAgent(agent);
                await CleanupDataOwner(owner);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConflictError.LinkedEntityExists))]
        public async Task CantDeleteOwnerWithPendingTransferRequest()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);
            try
            {
                var sourceOwnerResponse = await TestSetup.PdmsClientInstance.DataOwners.ReadAsync(
                                                    transferRequest.SourceOwnerId,
                                                    TestSetup.RequestContext,
                                                    DataOwnerExpandOptions.None)
                                                .ConfigureAwait(false);
                var sourceOwner = sourceOwnerResponse.Response;

                var ownerDeleteResponse = await TestSetup.PdmsClientInstance.DataOwners
                    .DeleteAsync(sourceOwner.Id, sourceOwner.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            }
            finally
            {
                await CleanupTransferRequest(transferRequest);
            }
        }

        [TestMethod]
        public async Task WhenICallApiToReadAllDataOwnersUsingHeadMethodItFailsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("api/v2/DataOwners", HttpStatusCode.BadRequest, HttpMethod.Head)
                .ConfigureAwait(false);
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }

        [TestMethod]
        public async Task WhenIUpdateADataOwnerWhenAssetIsInPendingTransferItSucceedsAsync()
        {
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            owner.Description = "new Description";
            owner.Name = Guid.NewGuid().ToString();
            owner.WriteSecurityGroups = new List<string>
                {
                    Guid.NewGuid().ToString()
                };
            owner.AlertContacts = new List<string>()
                {
                    "adgpdmsauditdri@microsoft.com"
                };
            owner.AnnouncementContacts = new List<string>()
                {
                    "ngpcieng@microsoft.com"
                };
            owner.HasInitiatedTransferRequests = true;
            var response = await TestSetup.PdmsClientInstance.DataOwners.UpdateAsync(
                    owner,
                    TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.AreEqual(response.HttpStatusCode, HttpStatusCode.OK);

            Assert.AreEqual(response.Response.Id, owner.Id);
            Assert.AreEqual(response.Response.Description, owner.Description);
            Assert.AreEqual(response.Response.Name, owner.Name);
            Assert.AreEqual(response.Response.WriteSecurityGroups.First(), owner.WriteSecurityGroups.First());
            Assert.AreEqual(response.Response.AlertContacts.First(), owner.AlertContacts.First());
            Assert.AreEqual(response.Response.AnnouncementContacts.First(), owner.AnnouncementContacts.First());

            await CleanupDataOwner(response.Response);
        }
    }
}
