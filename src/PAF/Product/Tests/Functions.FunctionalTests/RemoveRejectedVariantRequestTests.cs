namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.Common;
    using Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.PdmsService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Remove Rejected VariantRequest Work item tests.
    /// </summary>
    [TestClass]
    public class RemoveRejectedVariantRequestTests
    {
        private const string ComponentName = nameof(RemoveRejectedVariantRequestTests);

        private readonly ILogger logger;
        private readonly IVariantRequestWorkItemService workItemService;
        private readonly PdmsTestService pdmsTestService;
        private readonly IHttpClientWrapper httpClientWrapper;
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IAdoClientWrapper adoClientWrapper;

        private readonly PafLocalConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveRejectedVariantRequestTests"/> class.
        /// </summary>
        public RemoveRejectedVariantRequestTests()
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

            this.configuration = config;
        }

        /// <summary>
        /// Test whether the a work item is properly rejected.
        /// </summary>
        /// <returns> <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CheckForRemoveRejectedWorkItemSuccessAsync()
        {
            // Create a variant request
            var variantRequest = await this.pdmsTestService.CreateNewVariantRequestAsync().ConfigureAwait(false);

            Assert.IsNotNull(variantRequest, "Failed to create VariantRequest");

            var variantRequestId = variantRequest.Id;

            int workItemId = -1;
            try
            {
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

                    if (updatedRequest?.WorkItemUri == null)
                    {
                        // Force the create function to trigger
                        string fcnApiUrl = $"admin/functions/CreateVariantRequestWorkItem";
                        var message = new VariantRequestMessage() { VariantRequestId = variantRequestId };
                        var result = await new FunctionTrigger(this.logger, this.configuration).InvokeAsync<VariantRequestMessage>(fcnApiUrl, message).ConfigureAwait(false);
                    }
                }
                while (retryCount++ < 10 && updatedRequest?.WorkItemUri == null);

                Assert.IsNotNull(updatedRequest, "Update to VariantRequest failed.");
                Assert.IsNotNull(updatedRequest.WorkItemUri, "Update to VariantRequest WorkItemUri failed.");

                var workItemUrlParts = updatedRequest.WorkItemUri.ToString().Split('/');
                this.logger.Information(ComponentName, $"#work item parts = {workItemUrlParts.Length}");

                Assert.IsTrue(int.TryParse(workItemUrlParts[workItemUrlParts.Length - 1], out workItemId));

                // Get the variant request work item
                var workItem = await this.adoClientWrapper.GetWorkItemAsync(workItemId).ConfigureAwait(false);

                Assert.IsNotNull(workItem);
                Assert.AreEqual(workItem.Id, workItemId);

                // Reject the work item
                await this.workItemService.SetVariantRequestStateAsync(workItem.Id ?? default, "Rejected").ConfigureAwait(false);

                // Trigger the Remove function
                string fcnUrl = "admin/functions/RemoveRejectedVariantRequest";
                var success = await new FunctionTrigger(this.logger, this.configuration).InvokeAsync(fcnUrl, string.Empty).ConfigureAwait(false);

                // Give the Timer function a chance to run
                WorkItem updatedWorkItem;
                string state = string.Empty;
                do
                {
                    // sleep 30 seconds
                    Thread.Sleep(30 * 1000);

                    updatedWorkItem = await this.adoClientWrapper.GetWorkItemAsync(workItemId).ConfigureAwait(false);
                    state = updatedWorkItem.Fields.GetValueOrDefault("System.State") as string;
                }
                while (retryCount++ < 10 && state != "Removed");

                // Verify that the state is Removed
                Assert.AreEqual("Removed", state, "State set to Removed");

                // Verify that the variant request was deleted
                await this.pdmsTestService.VerifyVariantRequestDoesNotExistAsync(variantRequestId).ConfigureAwait(false);
            }
            finally
            {
                // Cleanup
                try
                {
                    // Delete the newly created workitem
                    if (workItemId != -1)
                    {
                        await this.workItemService.DeleteVariantRequestWorkItemAsync(workItemId, true).ConfigureAwait(false);
                    }

                    // Delete the variant request (and other resources) that we created
                    await this.pdmsTestService.CleanupVariantRequestAsync(variantRequest).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Ignore errors on cleanup
                    this.logger.Information(ComponentName, ex.Message);
                }
            }
        }
    }
}
