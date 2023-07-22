namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VariantRequestWorkItemServiceTests
    {
        private readonly ILogger logger;
        private readonly IVariantRequestWorkItemService workItemService;
        private readonly IFunctionConfiguration functionConfiguration;
        private readonly ExtendedVariantRequest variantRequest;
        private readonly ExtendedAssetGroupVariant variant;
        private readonly VariantRelationship assetGroup;

        private readonly string teamProject;

        public VariantRequestWorkItemServiceTests()
        {
            this.logger = DualLogger.Instance;

            var configFiles = new List<string>() { Path.Combine("Config", "local.settings.json") };
            var env = Environment.GetEnvironmentVariable("PAF_TestEnvironmentName", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(env))
            {
                configFiles.Add(Path.Combine("Config", $"{env}.settings.json"));
            }

            var configBuilder = new PafLocalConfigurationBuilder(configFiles);
            this.functionConfiguration = configBuilder.Build();
            var patchSerializer = new VariantRequestPatchSerializer();

            this.workItemService = new VariantRequestWorkItemService(this.functionConfiguration, new AdoClientWrapper(this.functionConfiguration, this.logger), this.logger, patchSerializer);

            this.variant = new ExtendedAssetGroupVariant()
            {
                VariantId = Guid.NewGuid().ToString(),
                VariantName = "Variant",
                EgrcId = "EXC-9999",
                EgrcName = "EgrcName"
            };

            this.assetGroup = new VariantRelationship()
            {
                AssetGroupId = Guid.NewGuid().ToString(),
                AssetQualifier = "AssetQualifier"
            };

            this.variantRequest = new ExtendedVariantRequest()
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = Guid.NewGuid().ToString(),
                OwnerName = "ownerName",
                GeneralContractorAlias = "pafadotestaccount",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
                VariantRelationships = new List<VariantRelationship>() { this.assetGroup },
                RequestedVariants = new List<ExtendedAssetGroupVariant>() { this.variant }
            };

            this.teamProject = this.functionConfiguration.AzureDevOpsProjectName;
        }

        [TestMethod]
        public async Task CreateAndDeleteWorkItemSuccessAsync()
        {
            // Create a variant request to test the workitem creation
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem, "WorkItem is Null");
                Console.WriteLine("WorkItem {0} Created", workItem.Id);

                var title = workItem.Fields.GetValueOrDefault("System.Title") as string;
                Assert.IsTrue(title.Contains(this.variant.EgrcId), "Title contains EgrcId");
                Assert.IsTrue(title.Contains(this.variant.EgrcName), "Title contains EgrcName");

                var description = workItem.Fields.GetValueOrDefault("System.Description") as string;
                Assert.IsTrue(description.Contains(this.variantRequest.OwnerName), "Description contains OwnerName");

                var htmlLink = workItem.Links.Links.GetValueOrDefault<string, object>("html") as ReferenceLink;
                Assert.IsTrue(htmlLink.Href.Contains(workItem?.Id.ToString()), "HtmlLink contains workItem Id");
                Console.WriteLine("WorkItem url = {0}", htmlLink.Href);
                this.logger.Information(nameof(VariantRequestWorkItemServiceTests), $"WorkItem url = {htmlLink.Href}");

                this.VerifyTeamProject(new List<WorkItem>() { workItem }, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DeleteWorkItemInWrongProjectFailsAsync()
        {
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                var config = this.functionConfiguration;
                var fakeConfig = new PafConfiguration()
                {
                    AMETenantId = config.AMETenantId,
                    AzureDevOpsAccessToken = config.AzureDevOpsAccessToken,
                    AzureDevOpsProjectUrl = config.AzureDevOpsProjectUrl,
                    AzureDevOpsProjectName = "IncorrectProject",
                    EnableNonProdFunctionality = config.EnableNonProdFunctionality,
                    MSTenantId = config.MSTenantId,
                    PafUamiId = config.PafUamiId,
                    PdmsResourceId = config.PdmsResourceId,
                    PdmsBaseUrl = config.PdmsBaseUrl,
                    ShouldUseAADToken = config.ShouldUseAADToken,
                    AadClientCert = config.AadClientCert,
                    AadClientId = config.AadClientId
                };

                var faultyWorkItemService = new VariantRequestWorkItemService(fakeConfig, new AdoClientWrapper(fakeConfig, this.logger), this.logger, new VariantRequestPatchSerializer());
                await faultyWorkItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "GC Approved").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task WorkItemCreatedForLongVariantNameShallCreateSuccessfully()
        {
            ExtendedAssetGroupVariant variantDefinition = new ExtendedAssetGroupVariant()
            {
                VariantId = Guid.NewGuid().ToString(),
                VariantName = "Variant",
                EgrcId = "EXC-9999",
                EgrcName = "EgrcName- If the length of EGRC Name is long then the ADO work item " +
                "title exceeds 256 characters and ADO is throwing an exception. In this case, we are trimming the work item to limit up to 256 characters"
            };

            ExtendedVariantRequest variantRequest = new ExtendedVariantRequest()
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = Guid.NewGuid().ToString(),
                OwnerName = "ownerName",
                GeneralContractorAlias = "gcalias",
                CelaContactAlias = "celaalias",
                RequesterAlias = "requesteralias",
                VariantRelationships = new List<VariantRelationship>() { this.assetGroup },
                RequestedVariants = new List<ExtendedAssetGroupVariant>() { variantDefinition }
            };

            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(variantRequest).ConfigureAwait(false);

            try
            {
                Assert.IsNotNull(workItem, "WorkItem is Null");
                Console.WriteLine("WorkItem {0} Created", workItem.Id);

                var title = workItem.Fields.GetValueOrDefault("System.Title") as string;
                Assert.AreEqual(256, title.Length);
                Assert.IsTrue(title.Contains(this.variant.EgrcId), "Title contains EgrcId");
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckUpdateAssignedToWithoutDomainSuccessAsync()
        {
            // Create a variant request workitem so that we can update it
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);

            try
            {
                Assert.IsNotNull(workItem);

                // Check that the state was updated
                var updatedWorkItem = await this.workItemService.UpdateWorkItemStateAsync(workItem.Id ?? default, "Active").ConfigureAwait(false);
                var updatedState = new KeyValuePair<string, object>("System.State", "Active");
                Assert.IsTrue(updatedWorkItem.Fields.Contains(updatedState), "The state of the WorkItem was not updated");

                // We need a real user id for this call, otherwise it throws an exception; currently using Richard.Roy (riro)
                updatedWorkItem = await this.workItemService.UpdateWorkItemAssignedToAsync(workItem.Id ?? default, "riro").ConfigureAwait(false);
                Assert.IsNotNull(updatedWorkItem, "The AssignedTo field update failed.");

                var identity = updatedWorkItem.Fields.GetValueOrDefault("System.AssignedTo") as IdentityRef;
                Assert.IsNotNull(identity, "The assignedTo field of the WorkItem was not updated.");
                Assert.AreEqual("riro@microsoft.com", identity.UniqueName);

                this.VerifyTeamProject(new List<WorkItem>() { updatedWorkItem }, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckUpdateAssignedToWithDomainSuccessAsync()
        {
            // Create a variant request workitem so that we can update it
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);

            try
            {
                Assert.IsNotNull(workItem);

                // Check that the state was updated
                var updatedWorkItem = await this.workItemService.UpdateWorkItemStateAsync(workItem.Id ?? default, "Active").ConfigureAwait(false);
                var updatedState = new KeyValuePair<string, object>("System.State", "Active");
                Assert.IsTrue(updatedWorkItem.Fields.Contains(updatedState), "The state of the WorkItem was not updated");

                // We need a real user id for this call, otherwise it throws an exception; currently using Richard.Roy (riro@microsoft.com)
                updatedWorkItem = await this.workItemService.UpdateWorkItemAssignedToAsync(workItem.Id ?? default, "riro@microsoft.com").ConfigureAwait(false);
                Assert.IsNotNull(updatedWorkItem, "The AssignedTo field update failed.");

                // If the AssignedTo field was added, then the update was successful
                var identity = updatedWorkItem.Fields.GetValueOrDefault("System.AssignedTo") as IdentityRef;
                Assert.IsNotNull(identity, "The assignedTo field of the WorkItem was not updated.");
                Assert.AreEqual("riro@microsoft.com", identity.UniqueName);

                this.VerifyTeamProject(new List<WorkItem>() { updatedWorkItem }, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckForPendingVariantRequestWorkItemSuccessAsync()
        {
            // Create a variant request  work item
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem);

                await this.workItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "CELA Approved").ConfigureAwait(false);

                var unapprovedWorkItems = await this.workItemService.GetPendingVariantRequestWorkItemsAsync().ConfigureAwait(false);
                Assert.IsTrue(this.ContainsWorkItemId(unapprovedWorkItems, workItem.Id ?? default), "The list of pending variant requests did not have the workitem id");

                this.VerifyTeamProject(unapprovedWorkItems, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckForPendingVariantRequestWorkItemFailureAsync()
        {
            // Create a variant request work item
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem);

                await this.workItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "Rejected").ConfigureAwait(false);

                var unapprovedWorkItems = await this.workItemService.GetPendingVariantRequestWorkItemsAsync().ConfigureAwait(false);
                Assert.IsFalse(this.ContainsWorkItemId(unapprovedWorkItems, workItem.Id ?? default), "The list of pending variant requests should not have this workitem id");

                this.VerifyTeamProject(unapprovedWorkItems, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckForRejectedVariantRequestSuccessAsync()
        {
            // Create a variant request work item
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem);

                await this.workItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "Rejected").ConfigureAwait(false);

                var rejectedWorkItems = await this.workItemService.GetRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);
                Assert.IsTrue(this.ContainsWorkItemId(rejectedWorkItems, workItem.Id ?? default), "The list of rejected variant requests did not the workitem id");

                this.VerifyTeamProject(rejectedWorkItems, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CheckForRejectedVariantRequestFailureAsync()
        {
            // Create a variant request work item
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem);

                await this.workItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "CELA Approved").ConfigureAwait(false);

                var rejectedWorkItems = await this.workItemService.GetRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);
                Assert.IsFalse(this.ContainsWorkItemId(rejectedWorkItems, workItem.Id ?? default), "The list of rejected variant requests should not have this workitem id");

                this.VerifyTeamProject(rejectedWorkItems, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CreateAndDeleteWorkItemWithMultipleVariants()
        {
            var anotherVariant = new ExtendedAssetGroupVariant()
            {
                VariantId = Guid.NewGuid().ToString(),
                VariantName = "AnotherVariant",
                EgrcId = "EXC-9999",
                EgrcName = "EgrcName"
            };

            this.variantRequest.RequestedVariants = new List<ExtendedAssetGroupVariant>() { this.variant, anotherVariant };
            
            // Create a variant request to test the workitem creation
            var workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(this.variantRequest).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem, "WorkItem is Null");
                Console.WriteLine("WorkItem {0} Created", workItem.Id);

                var title = workItem.Fields.GetValueOrDefault("System.Title") as string;
                Assert.IsTrue(title.Contains(this.variant.EgrcId), "Title contains EgrcId");
                Assert.IsTrue(title.Contains(this.variant.EgrcName), "Title contains EgrcName");

                var description = workItem.Fields.GetValueOrDefault("System.Description") as string;
                Assert.IsTrue(description.Contains(this.variantRequest.OwnerName), "Description contains OwnerName");

                var htmlLink = workItem.Links.Links.GetValueOrDefault<string, object>("html") as ReferenceLink;
                Assert.IsTrue(htmlLink.Href.Contains(workItem?.Id.ToString()), "HtmlLink contains workItem Id");
                Console.WriteLine("WorkItem url = {0}", htmlLink.Href);
                this.logger.Information(nameof(VariantRequestWorkItemServiceTests), $"WorkItem url = {htmlLink.Href}");

                this.VerifyTeamProject(new List<WorkItem>() { workItem }, this.teamProject);
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItem.Id ?? default, true).ConfigureAwait(false);
            }
        }

        private bool ContainsWorkItemId(List<WorkItem> workItems, int workItemId)
        {
            if (workItems != null)
            {
                foreach (var workItem in workItems)
                {
                    if (workItem.Id == workItemId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void VerifyTeamProject(List<WorkItem> workItems, string expectedTeamProject)
        {
            if (workItems != null)
            {
                foreach (var workItem in workItems)
                {
                    var teamProject = workItem.Fields.GetValueOrDefault("System.TeamProject") as string;
                    Assert.IsNotNull(teamProject, "Unexpected Team Project.");
                    Assert.AreEqual(expectedTeamProject, teamProject);
                }
            }
        }
    }
}
