// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <inheritdoc/>
    public class ScopedDeleteService : CoreService, IScopedDeleteService
    {
        private readonly IPcfProxyService pcfProxyService;
        private readonly IDictionary<string, string> siteIdToCallerName;
        private readonly string pcfCloudInstance;
        private readonly IAppConfiguration appConfiguration;
        private readonly ILogger logger;

        public ScopedDeleteService(
            IPxfDispatcher pxfDispatcher,
            IPcfProxyService pcfProxyService,
            IPrivacyConfigurationManager configurationManager,
            IAppConfiguration appConfiguration,
            ILogger logger)
            : base(pxfDispatcher)
        {
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService)); ;

            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }

            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.siteIdToCallerName = configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName;
            this.pcfCloudInstance = configurationManager.PrivacyExperienceServiceConfiguration.CloudInstance.ToPcfCloudInstance();
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse> SearchRequestsAndQueryScopedDeleteAsync(IRequestContext requestContext, IEnumerable<string> searchRequestsAndQueryIds)
        {
            // Validate input
            MsaSubject subject = null;
            IEnumerable<SearchRequestsAndQueryPredicate> predicates = null;
            Error error = null;

            if (!this.TryGetSubject(requestContext, out subject, out error))
            {
                return new ServiceResponse() { Error = error };
            }

            if (!this.TryParseSearchRequestsAndQueryPredicatesOrDefault(searchRequestsAndQueryIds, out predicates, out error))
            {
                return new ServiceResponse() { Error = error };
            }

            Portal portal = requestContext.GetPortal(siteIdToCallerName);

            // Process PCF requests
            List<DeleteRequest> requests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                subject: subject,
                requestContext: requestContext,
                requestGuid: LogicalWebOperationContext.ServerActivityId,
                correlationVector: Sll.Context?.Vector?.Value ?? string.Empty,
                context: string.Empty,
                timestamp: DateTimeOffset.UtcNow,
                predicates: predicates,
                privacyDataType: Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value,
                startTime: DateTimeOffset.MinValue,
                endTime: DateTimeOffset.UtcNow,
                cloudInstance: this.pcfCloudInstance,
                portal: portal.ToString(),
                isTest: false).ToList();

            // Using appConfig to control the delete requests batch size, approximation is: 1 request = 1K bytes
            // Keeping default as 200Requests can be handled at PCF considering no compression while posting to the eventHub.
            // Currently even with compression enabled, we saw several errors because original request size is way too big.
            List<List<DeleteRequest>> deleteRequestChunks = this.BuildChunksWithRange(requests, 
                this.appConfiguration.GetConfigValue<int>(ConfigNames.PXS.ScopedDeleteService_DeleteRequestsBatchSize, 200));


            foreach (var request in deleteRequestChunks)
            {
                ServiceResponse<IList<Guid>> pcfResponse = await this.pcfProxyService.PostDeleteRequestsAsync(requestContext, request).ConfigureAwait(false);

                if (!pcfResponse.IsSuccess)
                {
                    var errorDetails = new StringBuilder();
                    if (pcfResponse.Error != null)
                    {
                        errorDetails.Append($"errorMessage: {pcfResponse.Error.Message}, errorDetails: {pcfResponse.Error.ErrorDetails}");
                    }

                    this.logger.Error(nameof(ScopedDeleteService), $"Fail to post a chunk to PCF, {errorDetails}");
                    return new ServiceResponse() { Error = pcfResponse.Error };
                }
            }

            // Process PDOS deletion
            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();
            IEnumerable<DeleteResourceResponse> pxfDeletionResponses;

            if (predicates == null || !predicates.Any())
            {
                DeletionResponse<DeleteResourceResponse> bulkDeleteReponse = await this.PxfDispatcher.DeleteSearchHistoryAsync(pxfRequestContext, false);
                pxfDeletionResponses = bulkDeleteReponse.Items;
            }
            else
            {
                pxfDeletionResponses = await this.PxfDispatcher.ExecuteForProvidersAsync(
                    pxfRequestContext,
                    ResourceType.Search,
                    PxfAdapterCapability.Delete,
                    async a =>
                    {
                        if (!(a.Adapter is ISearchHistoryV2Adapter v2Adapter))
                            throw new NotSupportedException("Cannot delete single row with old adapters");
                        return await v2Adapter
                            .DeleteSearchHistoryAsync(
                                pxfRequestContext,
                                predicates.Select(p => new SearchV2Delete(p.ImpressionGuid)).ToArray())
                            .ConfigureAwait(false);
                    });
            }

            IEnumerable<DeleteResourceResponse> errors = pxfDeletionResponses.Where(x => x.Status == ResourceStatus.Error);

            if (errors.Any())
            {
                return new ServiceResponse
                {
                    Error = new Error(
                        ErrorCode.PartnerError,
                        string.Join(Environment.NewLine, errors.Select(e => $"[{e.PartnerId}] {e.Status}: {e.ErrorMessage}")))
                };
            }
            else
            {
                return new ServiceResponse();
            }
        }

        /// <summary>
        /// Creates chunks from fullList depending upon the batchSize.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalList">Original list.</param>
        /// <param name="batchSize">Batch size.</param>
        /// <returns>List of chunks based on batchSize.</returns>
        private List<List<T>> BuildChunksWithRange<T>(List<T> originalList, int batchSize)
        {
            List<List<T>> chunkedList = new List<List<T>>();
            int index = 0;

            while (index < originalList.Count)
            {
                int remaining = originalList.Count - index;
                if (remaining >= batchSize)
                {
                    chunkedList.Add(originalList.GetRange(index, batchSize));
                }
                else
                {
                    chunkedList.Add(originalList.GetRange(index, remaining));
                }
                index += batchSize;
            }

            this.logger.Information(nameof(ScopedDeleteService), $"OriginalDeleteRequestsCount: {originalList.Count}, DeleteRequestsBatchSize: {batchSize}, TotalChunks: {chunkedList.Count}");
            return chunkedList;
        }

        private bool TryGetSubject(IRequestContext requestContext, out MsaSubject subject, out Error error)
        {
            subject = null;
            error = null;

            if (requestContext == null || requestContext.Identity == null)
            {
                error = new Error(ErrorCode.InvalidInput, "Request identity cannot be null");
                return false;
            }

            if ((requestContext.Identity as AadIdentityWithMsaUserProxyTicket)?.UserProxyTicket == null &&
                (requestContext.Identity as MsaSelfIdentity)?.UserProxyTicket == null)
            {
                error = new Error(ErrorCode.InvalidClientCredentials, "Missing user proxy ticket.");
                return false;
            }

            subject = PrivacyRequestConverter.CreateMsaSubjectFromContext(requestContext);
            return true;
        }

        private bool TryParseSearchRequestsAndQueryPredicatesOrDefault(IEnumerable<string> searchRequestsAndQueryIds, out IEnumerable<SearchRequestsAndQueryPredicate> predicates, out Error error)
        {
            predicates = null; // Default
            error = null;

            if (searchRequestsAndQueryIds == null || !searchRequestsAndQueryIds.Any())
            {
                return true;
            }

            var result = new List<SearchRequestsAndQueryPredicate>();

            foreach (string id in searchRequestsAndQueryIds)
            {
                if (Guid.TryParse(id, out Guid guid))
                {
                    result.Add(new SearchRequestsAndQueryPredicate() { ImpressionGuid = guid.ToString("N").ToUpperInvariant() });
                }
                else
                {
                    error = new Error(ErrorCode.InvalidInput, $"Id {id} is not a guid.");
                    return false;
                }
            }

            predicates = result;
            return true;
        }
    }
}
