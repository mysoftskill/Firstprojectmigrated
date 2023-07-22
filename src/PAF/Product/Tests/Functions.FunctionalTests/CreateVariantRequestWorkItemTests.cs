namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.PdmsService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Creates VariantRequest Work item tests.
    /// </summary>
    [TestClass]
    public class CreateVariantRequestWorkItemTests
    {
        private const string ComponentName = nameof(CreateVariantRequestWorkItemTests);

        private readonly ILogger logger;
        private readonly IVariantRequestWorkItemService workItemService;
        private readonly PdmsTestService pdmsTestService;
        private readonly IHttpClientWrapper httpClientWrapper;
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IAdoClientWrapper adoClientWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateVariantRequestWorkItemTests"/> class.
        /// </summary>
        public CreateVariantRequestWorkItemTests()
        {
            this.logger = DualLogger.Instance;

            var configFiles = new List<string>() { Path.Combine("Config", "local.settings.json") };
            var env = Environment.GetEnvironmentVariable("PAF_TestEnvironmentName", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(env))
            {
                configFiles.Add(Path.Combine("Config", $"{env}.settings.json"));
            }

            var configBuilder = new PafLocalConfigurationBuilder(configFiles);
            var config = configBuilder.Build() as PafLocalConfiguration;

            var patchSerializer = new VariantRequestPatchSerializer();

            this.adoClientWrapper = new AdoClientWrapper(config, this.logger);
            this.workItemService = new VariantRequestWorkItemService(config, this.adoClientWrapper, this.logger, patchSerializer);

            // Token provider for calls to PdmsService via HttpClientWrapper
            var reader = new SecretsReader(config.PafClientId, config.AMETenantId, config.CertificateSubjectName, config.PdmsKeyVaultUrl, "certificates/" + config.PdmsCertName);
            var cert = reader.GetCertificateByNameAsync(config.PdmsCertName).GetAwaiter().GetResult();
            this.authenticationProvider = new ClientSecretProvider(
                config.PdmsClientId,
                config.AMETenantId,
                config.PdmsResourceId,
                cert);

            this.httpClientWrapper = new HttpClientWrapper(this.logger, config.PdmsBaseUrl);

            // Initialize helper service for accessing additional PDMS functionality
            this.pdmsTestService = new PdmsTestService(config.PdmsClientId, cert, config.PdmsBaseUrl, this.logger);
        }

        /// <summary>
        /// Test whether the function is called when a new variant request is created.
        /// </summary>
        /// <returns><see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CheckForCreateWorkItemSuccessAsync()
        {
            // Create a variant request
            var variantRequest = await this.pdmsTestService.CreateNewVariantRequestAsync().ConfigureAwait(false);

            Assert.IsNotNull(variantRequest, "Failed to create VariantRequest");

            var variantRequestId = variantRequest.Id;

            // Loop waiting for the work item url to be updated;
            // Allow up to 5 minutes for the work item to be created.
            var retryCount = 0;
            VariantRequest updatedRequest;
            do
            {
                // sleep 30 seconds
                Thread.Sleep(30 * 1000);

                string apiUrl = $"api/v2/variantRequests('{variantRequestId}')";
                updatedRequest = await this.httpClientWrapper.GetAsync<VariantRequest>(apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger)).ConfigureAwait(false);
            }
            while (retryCount++ < 10 && updatedRequest?.WorkItemUri == null);

            Assert.IsNotNull(updatedRequest, "Update to VariantRequest failed.");
            Assert.IsNotNull(updatedRequest.WorkItemUri, "Update to VariantRequest WorkItemUri failed.");

            var workItemUrlParts = updatedRequest.WorkItemUri.ToString().Split('/');
            this.logger.Information(ComponentName, $"#work item parts = {workItemUrlParts.Length}");

            Assert.IsTrue(int.TryParse(workItemUrlParts[workItemUrlParts.Length - 1], out int workItemId));

            // Get the variant request work item
            var workItem = await this.adoClientWrapper.GetWorkItemAsync(workItemId).ConfigureAwait(false);
            try
            {
                Assert.IsNotNull(workItem);
                Assert.AreEqual(workItem.Id, workItemId);

                // Verify that the title contains the egrc id and name
                var variant = updatedRequest.RequestedVariants.FirstOrDefault();
                var title = workItem.Fields.GetValueOrDefault("System.Title") as string;
                Assert.IsTrue(title.Contains(variant.EgrcId), "Title does not contains EgrcId");
                Assert.IsTrue(title.Contains(variant.EgrcName), "Title does not contains EgrcName");

                // Verify that the description contains the pcd team name
                var description = workItem.Fields.GetValueOrDefault("System.Description") as string;
                Assert.IsTrue(description.Contains(updatedRequest.OwnerName), "Description does not contains OwnerName");
                Assert.IsTrue(description.Contains("ServiceTree ID") && description.Contains("Organization Name") && description.Contains("Additional Information"), "Additional Information is not available in ADO WorkItem");

                // Verify that variants table contains DataTypes, Subject Types and Capabilities
                var variantsTable = workItem.Fields.GetValueOrDefault("Custom.ListofVariantsDescrip") as string;
                Assert.IsTrue(variantsTable.Contains(Policies.Current.DataTypes.Ids.Account.Value), "DataType is not available in ADO work item");
                Assert.IsTrue(variantsTable.Contains(Policies.Current.SubjectTypes.Ids.DemographicUser.Value), "DataType is not available in ADO work item");
                Assert.IsTrue(variantsTable.Contains(Policies.Current.Capabilities.Ids.Delete.Value), "Capability is not available in ADO work item");

                // Verify that the state is Active
                var state = workItem.Fields.GetValueOrDefault("System.State") as string;
                Assert.AreEqual("Active", state, "State is Active");
            }
            finally
            {
                // Delete the newly created workitem
                await this.workItemService.DeleteVariantRequestWorkItemAsync(workItemId, true).ConfigureAwait(false);

                // Delete the variant request that we created
                await this.pdmsTestService.CleanupVariantRequestAsync(variantRequest).ConfigureAwait(false);
            }
        }
    }
}
