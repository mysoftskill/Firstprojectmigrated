namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.WebJobs;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.WebApi;
    using Newtonsoft.Json;

    /// <summary>
    /// Processing all variant request Azure Functions
    /// </summary>
    public class VariantRequestProcessor : IVariantRequestProcessor
    {
        private const string ComponentName = nameof(VariantRequestProcessor);
        private const string ALL = "ALL";
        private const string UNAVAILABLE = "UNAVAILABLE";
        private readonly IFunctionConfiguration configuration;
        private readonly IVariantRequestWorkItemService workItemService;
        private readonly IPdmsService pdmsService;
        private readonly ILogger logger;
        private readonly IMetricContainer metricContainer;
        private readonly IPafMapper mapper;
        private readonly string workItemUrlPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestProcessor"/> class.
        /// Constructor
        /// </summary>
        /// <param name="configuration">Implementation of IFunctionConfiguration</param>
        /// <param name="logger">Implementation of ILogger</param>
        /// <param name="workItemService">Implementation of IWorkItemService</param>
        /// <param name="pdmsService">Implementation of IPdmsService</param>
        /// <param name="metricContainer">Metric configuration</param>
        /// <param name="mapper">PafMapper</param>
        public VariantRequestProcessor(
            IFunctionConfiguration configuration,
            IVariantRequestWorkItemService workItemService,
            IPdmsService pdmsService,
            ILogger logger,
            IMetricContainer metricContainer,
            IPafMapper mapper)
        {
            this.configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            this.workItemService = workItemService ?? throw new ArgumentException(nameof(workItemService));
            this.pdmsService = pdmsService ?? throw new ArgumentException(nameof(pdmsService));
            this.logger = logger ?? throw new ArgumentException(nameof(logger));
            this.metricContainer = metricContainer ?? throw new ArgumentException(nameof(metricContainer));

            // Url path to use if we can't get it from the work item response.
            this.workItemUrlPrefix = $"{configuration.AzureDevOpsProjectUrl}/{configuration.AzureDevOpsProjectName}/_workitems/edit/";
            this.mapper = mapper;
        }

        /// <summary>
        /// Creates VariantRequest Work item
        /// </summary>
        /// <param name="variantRequestMessageJson">Message from the queue</param>
        /// <param name="processedQueue">processedQueue</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateVariantRequestWorkItemAsync(
            string variantRequestMessageJson,
            ICollector<string> processedQueue)
        {
            IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Create_Work_Item", this.logger, "Variant");
            try
            {
                // Start API event
                outgoingApi.Start();

                var variantRequestMessage = JsonConvert.DeserializeObject<VariantRequestMessage>(variantRequestMessageJson);
                var variantRequestIdFromMessage = variantRequestMessage?.VariantRequestId;

                if (!Guid.TryParse(variantRequestIdFromMessage, out Guid variantRequestId))
                {
                    this.logger.Error(ComponentName, $"Error parsing VariantRequestId: {variantRequestIdFromMessage}");
                    outgoingApi.Success = false;
                    throw new ArgumentException("Error parsing VariantRequestId", nameof(variantRequestIdFromMessage));
                }

                this.logger.Information(ComponentName, $"variant_requestId: {variantRequestIdFromMessage}");
                VariantRequest variantRequest = await this.pdmsService.GetVariantRequestAsync(variantRequestId).ConfigureAwait(false);

                // Check for duplicate request
                WorkItem workItem = null;
                if (variantRequest?.WorkItemUri == null)
                {
                    ExtendedVariantRequest extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);
                    extendedVariantRequest.RequestedVariants = this.UpdateVariantDefinitions(extendedVariantRequest.RequestedVariants);
                    this.logger.Information(ComponentName, $"Creating Work Item for variant request = {variantRequestMessage}");
                    workItem = await this.workItemService.CreateVariantRequestWorkItemAsync(extendedVariantRequest).ConfigureAwait(false);
                    this.logger.Information(ComponentName, $"Work Item {workItem?.Id} created for variant request {variantRequestIdFromMessage}");

                    // Update variant request with the WorkItem link
                    if (workItem != null)
                    {
                        // Get ADO url and add it to the variant request
                        string workItemUrl;
                        if (workItem.Links?.Links?.GetValueOrDefault<string, object>("html") is ReferenceLink htmlLink)
                        {
                            workItemUrl = htmlLink.Href;
                        }
                        else
                        {
                            workItemUrl = $"{this.workItemUrlPrefix}{workItem.Id}";
                        }

                        this.logger.Information(ComponentName, $"Update Variant Request Uri: {workItemUrl}");

                        variantRequest.WorkItemUri = new Uri(workItemUrl);

                        await this.pdmsService.UpdateVariantRequestAsync(variantRequest).ConfigureAwait(false);
                        outgoingApi.Success = true;
                    }
                    else
                    {
                        outgoingApi.Success = false;
                        throw new InvalidOperationException($"Unable to create work item for request id: {variantRequestIdFromMessage}");
                    }
                }
                else
                {
                    outgoingApi.Success = false;
                    this.logger.Warning(ComponentName, $"Variant request {variantRequestIdFromMessage} already has a work item associated with it: {variantRequest.WorkItemUri}");
                }

                // Put the processed request in an output queue for debugging
                processedQueue.Add($"variantRequestId = {variantRequestIdFromMessage}, workItemId = {workItem?.Id}, workItemUrl = {workItem?.Url}");
            }
            catch (Exception ex)
            {
                outgoingApi.Success = false;
                this.logger.Error(ComponentName, $"Error processing variant request = {variantRequestMessageJson}: {ex.Message}");
                throw;
            }
            finally
            {
                outgoingApi.Finish();
            }
        }

        /// <summary>
        /// Move variant requests to unprocessed queue and set metric dimensions
        /// </summary>
        /// <param name="variantRequestPoisonQueue">Poison queue name</param>
        /// <param name="variantRequestMessageJson">Message from the queue</param>
        /// <param name="unprocessedQueue">messages from poison queue move to this queue for reprocessing</param>
        public void MoveVariantRequestToUnprocessedQueueAsync(string variantRequestPoisonQueue, string variantRequestMessageJson, ICollector<string> unprocessedQueue)
        {
            IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Move_Request_To_Unprocessed_Queue", this.logger, "Variant");

            try
            {
                // Start API Event
                outgoingApi.Start();

                var variantRequestMessage = JsonConvert.DeserializeObject<VariantRequestMessage>(variantRequestMessageJson);
                var variantRequestIdFromMessage = variantRequestMessage?.VariantRequestId;

                if (!Guid.TryParse(variantRequestIdFromMessage, out Guid variantRequestId))
                {
                    this.logger.Error(ComponentName, $"Error parsing VariantRequestId: {variantRequestIdFromMessage}");
                    throw new ArgumentException("Error parsing VariantRequestId", nameof(variantRequestIdFromMessage));
                }

                this.logger.Information(ComponentName, $"variant_requestId: {variantRequestIdFromMessage}");

                string[] dimVal = new string[2] { variantRequestPoisonQueue, variantRequestIdFromMessage };
                string key = "PAF.FunctionVariantRequestPoisonQueue";
                if (this.metricContainer.CustomMetricDictionary.TryGetValue(key, out var metric))
                {
                    metric.SetUInt64Metric(1U, dimVal);
                }
                else
                {
                    throw new Exception($"CustomMetricDictionary does not contain metric with key {key}");
                }

                outgoingApi.Success = true;

                // Put the unprocessed variant request in an output queue for reprocessing
                unprocessedQueue.Add($"variantRequestId = {variantRequestIdFromMessage}");
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, $"Error running telemetry function: {ex.Message}");
                outgoingApi.Success = false;
                throw;
            }
            finally
            {
                // Finish API event
                outgoingApi.Finish();
            }
        }

        /// <summary>
        /// Updates PDMS with all approved workitems and sets the workitems to approved
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateApprovedVariantRequestWorkItemsAsync()
        {
            try
            {
                var pendingWorkItems = await this.workItemService.GetPendingVariantRequestWorkItemsAsync().ConfigureAwait(false);
                if (pendingWorkItems == null)
                {
                    this.logger.Information(ComponentName, "No pending work items found");
                }
                else
                {
                    this.logger.Information(ComponentName, $"There were {pendingWorkItems.Count} pending work items found");
                    foreach (var workItem in pendingWorkItems)
                    {
                        // Start API event for work item
                        IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Update_Approved", this.logger, "Variant");
                        Guid requestId = Guid.Empty;
                        try
                        {
                            outgoingApi.Start();

                            // Approve each workitem in PDMS
                            if (workItem.Fields.ContainsKey("Custom.VariantRequestId"))
                            {
                                // Attempt to approve the associated variant request
                                var requestIdFromWorkItem = workItem.Fields["Custom.VariantRequestId"].ToString();
                                if (Guid.TryParse(requestIdFromWorkItem, out requestId))
                                {
                                    if (await this.pdmsService.ApproveVariantRequestAsync(requestId).ConfigureAwait(false))
                                    {
                                        await this.workItemService.ApproveVariantRequestWorkItemAsync(workItem.Id ?? default).ConfigureAwait(false);
                                        this.logger.Information(ComponentName, $"Proccessed workitem {workItem.Id}");
                                        outgoingApi.Success = true;
                                    }
                                    else
                                    {
                                        this.logger.Error(ComponentName, $"Error unable to approve variantRequest {requestId} in PDMS");
                                        outgoingApi.Success = false;
                                    }
                                }
                                else
                                {
                                    this.logger.Error(ComponentName, $"Could not parse requestId in workitem {workItem.Id}: {requestIdFromWorkItem}");
                                    outgoingApi.Success = false;
                                }
                            }
                            else
                            {
                                this.logger.Error(ComponentName, $"Variant Request work item is missing a VariantRequestId");
                                outgoingApi.Success = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.logger.Error(ComponentName, $"Error approving variantRequest {requestId} or workitem {workItem.Id}: {ex.Message}");
                            outgoingApi.Success = false;
                            throw;
                        }
                        finally
                        {
                            // Finish API event for work item
                            outgoingApi.Finish();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, $"Error processing approved variant requests : {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes all rejected variant requests from PDMS and sets the workitem state to Removed
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RemoveRejectedVariantRequestWorkItemsAsync()
        {
            var rejectedWorkItems = await this.workItemService.GetRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);
            if (rejectedWorkItems == null)
            {
                this.logger.Information(ComponentName, "No rejected work items found");
            }
            else
            {
                this.logger.Information(ComponentName, $"There were {rejectedWorkItems.Count} rejected work items found");
                int removedCount = 0;
                foreach (var workItem in rejectedWorkItems)
                {
                    // Remove the associated variant request in PDMS
                    if (workItem.Fields.ContainsKey("Custom.VariantRequestId"))
                    {
                        // Start API event for custom work item
                        IApiEvent outgoingCustomApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Remove_Custom_Rejected", this.logger, "Variant");
                        outgoingCustomApi.Start();

                        // Attempt to delete the associated variant request
                        var requestIdFromWorkItem = workItem.Fields["Custom.VariantRequestId"].ToString();
                        if (Guid.TryParse(requestIdFromWorkItem, out Guid requestId))
                        {
                            try
                            {
                                if (!await this.pdmsService.DeleteVariantRequestAsync(requestId).ConfigureAwait(false))
                                {
                                    // TODO: Write this to a queue? Raise an alert?
                                    this.logger.Error(ComponentName, $"Error deleting variantRequest {requestId}");
                                    outgoingCustomApi.Success = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                this.logger.Error(ComponentName, $"Error deleting variantRequest {requestId} : {ex.Message}");
                                outgoingCustomApi.Success = false;
                            }
                            finally
                            {
                                // Finish API event for work item
                                outgoingCustomApi.Finish();
                            }
                        }
                        else
                        {
                            this.logger.Error(ComponentName, $"Could not parse requestId in workitem {workItem.Id}: {requestIdFromWorkItem}");
                            outgoingCustomApi.Success = false;

                            // Finish API event for work item
                            outgoingCustomApi.Finish();
                        }
                    }

                    // Start API event for work item
                    IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Remove_Rejected", this.logger, "Variant");
                    outgoingApi.Start();

                    // Set the state of the work item to Removed
                    try
                    {
                        if (workItem.Id.HasValue)
                        {
                            await this.workItemService.RemoveVariantRequestWorkItemAsync(workItem.Id.Value).ConfigureAwait(false);
                            outgoingApi.Success = true;
                        }
                        else
                        {
                            this.logger.Error(ComponentName, "WorkItem Id was null");
                            outgoingApi.Success = false;
                            throw new NullReferenceException("Workitem Id was null");
                        }

                        removedCount++;
                        this.logger.Information(ComponentName, $"Removed workitem {workItem.Id}");
                    }
                    catch (Exception ex)
                    {
                        // Log the fact that we didn't remove the work item, but don't rethrow the exception
                        // so that we process the rest of the workitems.
                        // TODO: set up alert for this; possibly write to a queue
                        this.logger.Error(ComponentName, $"Error removing workitem {workItem.Id}: {ex.Message}");
                        outgoingApi.Success = false;
                    }
                    finally
                    {
                        // Finish API event for work item
                        outgoingApi.Finish();
                    }
                }

                this.logger.Information(ComponentName, $"{removedCount} rejected work items were removed.");
            }
        }

        /// <summary>
        /// Update variants with additional variant definition information
        /// </summary>
        /// <param name="variants">A <see cref="IEnumerable{ExtendedAssetGroupVariant}"/> which denotes the list of variants that needs to be updated</param>
        /// <returns>A <see cref="IEnumerable{ExtendedAssetGroupVariant}"/></returns>
        private IEnumerable<ExtendedAssetGroupVariant> UpdateVariantDefinitions(IEnumerable<ExtendedAssetGroupVariant> variants)
        {
            return variants == null ? variants : variants.Select(variant =>
            {
                IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "Variant_Request_Processor", "Variant_Processor_Fetch_Variant_Definition_Failed", this.logger, "Variant");
                Guid variantDefinitionId = Guid.Empty;
                try
                {
                    // Start API event for selected variant
                    outgoingApi.Start();

                    if (!Guid.TryParse(variant.VariantId, out variantDefinitionId))
                    {
                        this.logger.Error(ComponentName, $"Error parsing variantDefinitionId: {variant.VariantId}");
                        throw new ArgumentException("Error parsing variantDefinitionId", nameof(variant.VariantId));
                    }

                    VariantDefinition variantDefinition = this.pdmsService.GetVariantDefinitionAsync(variantDefinitionId).Result;
                    variant.SubjectTypes = variantDefinition.SubjectTypes.Any() ? variantDefinition.SubjectTypes : variantDefinition.SubjectTypes.Append(ALL);
                    variant.DataTypes = variantDefinition.DataTypes.Any() ? variantDefinition.DataTypes : variantDefinition.DataTypes.Append(ALL);
                    variant.Capabilities = variantDefinition.Capabilities.Any() ? variantDefinition.Capabilities : variantDefinition.Capabilities.Append(ALL);
                    outgoingApi.Success = true;
                }
                catch (Exception ex)
                {
                    // Log to know that request to PDMS to fetch variant definition is failed and don't rethrow the exception
                    // so that we don't stop the creation of the work item.
                    this.logger.Error(ComponentName, $"Request to PDMS failed while fetching variant definition for {variantDefinitionId}: {ex.Message}");
                    variant.SubjectTypes = new List<string>() { UNAVAILABLE };
                    variant.DataTypes = new List<string>() { UNAVAILABLE };
                    variant.Capabilities = new List<string>() { UNAVAILABLE };
                    outgoingApi.Success = false;
                }
                finally
                {
                    // Finish API event for selected variant
                    outgoingApi.Finish();
                }

                return variant;
            }).ToList();
        }
    }
}
