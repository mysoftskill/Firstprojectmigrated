namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.PdmsService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FunctionalTestCleanup
    {
        private const int MaxWorkItemLifeInDays = 7;

        [AssemblyCleanup]
        public static async Task AssemblyCleanupAsync()
        {
            string environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

            // Avoid cleanup in local runs. It takes a some time.
            if (environment != null)
            {
                ILogger logger = DualLogger.Instance;
                List<string> configFiles = new List<string>() { Path.Combine("Config", "local.settings.json") };
                string env = Environment.GetEnvironmentVariable("PAF_TestEnvironmentName", EnvironmentVariableTarget.Process);
                if (!string.IsNullOrEmpty(env))
                {
                    configFiles.Add(Path.Combine("Config", $"{env}.settings.json"));
                }

                PafLocalConfigurationBuilder configBuilder = new PafLocalConfigurationBuilder(configFiles);
                IFunctionConfiguration functionConfiguration = configBuilder.Build();
                VariantRequestPatchSerializer patchSerializer = new VariantRequestPatchSerializer();

                PafLocalConfiguration config = functionConfiguration as PafLocalConfiguration;

                // cert for calls to PdmsService via HttpClientWrapper
                var reader = new SecretsReader(config.PafClientId, config.AMETenantId, config.CertificateSubjectName, config.PdmsKeyVaultUrl, "certificates/" + config.PdmsCertName);
                var cert = reader.GetCertificateByNameAsync(config.PdmsCertName).GetAwaiter().GetResult();

                PdmsTestService pdmsTestService = new PdmsTestService(config.PdmsClientId, cert, config.PdmsBaseUrl, logger);

                IVariantRequestWorkItemService workItemService = new VariantRequestWorkItemService(functionConfiguration, new AdoClientWrapper(functionConfiguration, logger), logger, patchSerializer);

                try
                {
                    List<int> listOfPendingWorkItems = await workItemService.GetAllWorkItemsIdsAsync().ConfigureAwait(false);

                    if (listOfPendingWorkItems != null)
                    {
                        // Create multiple subarrays to get work item details, sending too many ids at once throws exception
                        IEnumerable<IEnumerable<int>> subarrays = listOfPendingWorkItems.Select((s, i) => listOfPendingWorkItems.Skip(i * 100).Take(100)).Where(a => a.Any());

                        foreach (IEnumerable<int> ids in subarrays)
                        {
                            // Retrieve work item.
                            List<WorkItem> items = await workItemService.GetWorkItemsWithIdsAsync(ids).ConfigureAwait(false);
                            string varientRequestIdKey = "Custom.VariantRequestId";

                            foreach (WorkItem item in items)
                            {
                                // Delete work item if the variant request does not exists in pdms.
                                if (item.Fields.ContainsKey(varientRequestIdKey))
                                {
                                    string variantRequestId = item.Fields[varientRequestIdKey] as string;
                                    DateTime createdDate = (DateTime)item.Fields["System.CreatedDate"];
                                    TimeSpan elapsedTime = DateTime.UtcNow - createdDate;
                                    bool isWorkItemCreatedByFcts = false;

                                    if (item.Fields["Custom.ListofAssetGroups"] is string assetGroups)
                                    {
                                        Regex assetTypeRegex = new Regex(
                                            @"AssetType=CosmosStructuredStream;",
                                            RegexOptions.IgnoreCase);

                                        Regex clusterRegex = new Regex(@"PhysicalCluster=cosmos17;", RegexOptions.IgnoreCase);

                                        isWorkItemCreatedByFcts = assetTypeRegex.Matches(assetGroups).Count > 0
                                            && clusterRegex.Matches(assetGroups).Count > 0;
                                    }

                                    bool isWorkItemTooOld = (elapsedTime - TimeSpan.FromDays(MaxWorkItemLifeInDays)).TotalDays > 0;
                                    try
                                    {
                                        if (isWorkItemCreatedByFcts && (isWorkItemTooOld || await pdmsTestService.DoesVariantRequestExistAsync(variantRequestId).ConfigureAwait(false) == false))
                                        {
                                            Console.WriteLine($"Variant {variantRequestId} missing from PDMS, workItem will be deleted");
                                            await workItemService.DeleteVariantRequestWorkItemAsync(item.Id ?? default, true).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Variant {variantRequestId} exists in PDMS");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine($"Exception {e}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception {e}");
                }
            }
        }
    }
}
