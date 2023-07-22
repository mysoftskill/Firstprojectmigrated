namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using IdConverter = SocialAccessorV4.IdConverter;
    using PcfPrivacySubjects = Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// RecurrentDeleteScheduleWorker
    /// </summary>
    public class RecurrentDeleteScheduleWorker : BaseRecurringDeleteQueueWorker
    {
        private readonly IPcfProxyService pcfProxyService;
        private readonly IPxfDispatcher pxfDispatcher;
        private const string CallerName = "ADGCS PXS " + nameof(RecurrentDeleteScheduleWorker);

        /// <summary>
        /// RecurrentDeleteScheduleWorker
        /// </summary>
        /// <param name="cloudQueue"></param>
        /// <param name="cloudQueueConfiguration"></param>
        /// <param name="scheduleDbConfiguration"></param>
        /// <param name="appConfiguration"></param>
        /// <param name="scheduleDbClient"></param>
        /// <param name="pcfProxyService"></param>
        /// <param name="pxfDispatcher"></param>
        /// <param name="logger"></param>
        public RecurrentDeleteScheduleWorker(
            ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue,
            ICloudQueueConfiguration cloudQueueConfiguration,
            IScheduleDbConfiguration scheduleDbConfiguration,
            IAppConfiguration appConfiguration,
            IScheduleDbClient scheduleDbClient,
            IPcfProxyService pcfProxyService,
            IPxfDispatcher pxfDispatcher,
            ILogger logger) : base(cloudQueue, cloudQueueConfiguration, scheduleDbConfiguration, appConfiguration, scheduleDbClient, logger)
        {
            this.pcfProxyService = pcfProxyService;
            this.pxfDispatcher = pxfDispatcher;
        }

        public override async Task ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(
            RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument,
            OutgoingApiEventWrapper outgoingApi)
        {
            bool isRecurringDeleteWorkerEnabled =
            await this.appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.RecurringDeleteWorkerEnabled).ConfigureAwait(false);

            if (!isRecurringDeleteWorkerEnabled)
            {
                this.logger.Warning(this.componentName, $"ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync. {FeatureNames.PXS.RecurringDeleteWorkerEnabled} is disabled.");
                outgoingApi.ExtraData[FeatureNames.PXS.RecurringDeleteWorkerEnabled] = false.ToString();
                return;
            }

            try
            {
                IRequestContext requestContext = this.RecurrentDeleteScheduleDbDocumentToRequestContext(recurrentDeleteScheduleDbDocument);
                DeleteRequest deleteRequest = CreatePcfDeleteRequests(recurrentDeleteScheduleDbDocument, requestContext);
                ServiceResponse serviceResponse = await this.pcfProxyService.PostMsaRecurringDeleteRequestsAsync(requestContext, deleteRequest, recurrentDeleteScheduleDbDocument.PreVerifier).ConfigureAwait(false);
                if (serviceResponse.Error != null)
                {
                    this.logger.Error(this.componentName, $"Fail to send to PCF: {serviceResponse.Error}");
                    throw new InvalidOperationException($"Fail to send to PCF: {serviceResponse.Error}");
                }

                // Send delete requests to PDOS
                IPxfRequestContext pxfRequestContext = this.RecurrentDeleteScheduleDbDocumentToPxfRequestContext(recurrentDeleteScheduleDbDocument);
                var deletePolicyDataTypeTask = this.pxfDispatcher.CreateDeletePolicyDataTypeTask(recurrentDeleteScheduleDbDocument.DataType, pxfRequestContext);
                if (deletePolicyDataTypeTask == null)
                {
                    this.logger.Warning(this.componentName, $"The data type:{recurrentDeleteScheduleDbDocument.DataType} doesn't match with any pdos api. No task is created. Document id={recurrentDeleteScheduleDbDocument.DocumentId}");
                }
                else
                {
                    var pxfServiceResponse = await deletePolicyDataTypeTask.ConfigureAwait(false);
                    List<DeleteResourceResponse> errors = pxfServiceResponse.Items.Where(r => r.Status != ResourceStatus.Deleted).ToList();
                    if (errors.Count > 0)
                    {
                        serviceResponse.Error = new Error(
                                ErrorCode.PartnerError,
                                string.Join(Environment.NewLine, errors.Select(e => $"[{e.PartnerId}] {e.Status}: {e.ErrorMessage}")));
                        this.logger.Error(this.componentName, $"Fail to send to PDOS: {serviceResponse.Error}");
                        throw new InvalidOperationException($"Fail to send to PDOS: {serviceResponse.Error}");
                    }
                }

                // Add NextDeleteOccurrence with current interval days
                await this.UpdateScheduleDbAsync(recurrentDeleteScheduleDbDocument, 
                    doc =>
                    {
                        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                        if (doc.RecurringIntervalDays.HasValue)
                        {
                            doc.NextDeleteOccurrenceUtc = utcNow.AddDays((int)doc.RecurringIntervalDays.Value);
                        }
                        doc.LastDeleteOccurrenceUtc = utcNow;
                        doc.LastSucceededDeleteOccurrenceUtc = utcNow;
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException))
                {
                    this.logger.Error(this.componentName, ex, $"Exception occured while processing RecurrentDeleteScheduleDbDocument: {ex.Message}");
                }
                // update scheduleDbDoc for retry
                await this.UpdateScheduleDbAsync(recurrentDeleteScheduleDbDocument,
                    doc =>
                    {
                        if (doc.NumberOfRetries >= this.scheduleDbConfig.MaxNumberOfRetries)
                        {
                            doc.RecurrentDeleteStatus = RecurrentDeleteStatus.Paused;
                        }
                        else
                        {
                            doc.NumberOfRetries++;
                        }
                        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                        doc.LastDeleteOccurrenceUtc = utcNow;
                        doc.NextDeleteOccurrenceUtc = utcNow.AddMinutes(this.scheduleDbConfig.RetryTimeDurationMinutes);
                    }).ConfigureAwait(false);
                throw;
            }
        }

        private IRequestContext RecurrentDeleteScheduleDbDocumentToRequestContext(RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument)
        {
            return new RequestContext(
                new MsaSelfIdentity(
                userProxyTicket: null,
                familyJsonWebToken: null,
                authorizingPuid: recurrentDeleteScheduleDbDocument.Puid,
                targetPuid: recurrentDeleteScheduleDbDocument.Puid,
                userAuthorizingCid: 0,
                callerName: CallerName,
                siteId: 0,
                targetCid: 0,
                targetCountryRegion: null,
                null,
                false,
                AuthType.MsaSelf));
        }

        private IPxfRequestContext RecurrentDeleteScheduleDbDocumentToPxfRequestContext(RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument)
        {
            return new PxfRequestContext(
                userProxyTicket: null,
                familyJsonWebToken: null,
                authorizingPuid: recurrentDeleteScheduleDbDocument.Puid,
                targetPuid: recurrentDeleteScheduleDbDocument.Puid,
                targetCid: null,
                countryRegion: null,
                isWatchdogRequest: false,
                flights: new string[0]);
        }

        private DeleteRequest CreatePcfDeleteRequests(RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument, IRequestContext requestContext)
        {
            var deleteRequest = new DeleteRequest
            {
                Predicate = null,
                TimeRangePredicate = new TimeRangePredicate
                {
                    StartTime = DateTimeOffset.MinValue,
                    EndTime = DateTimeOffset.UtcNow,
                },
                PrivacyDataType = recurrentDeleteScheduleDbDocument.DataType,
            };

            if (!(requestContext.Identity is MsaSelfIdentity))
            {
                throw new ArgumentOutOfRangeException(nameof(requestContext.Identity), $"Unexpected identity type: {requestContext.Identity.GetType().FullName}");
            }

            var msaSubject = new PcfPrivacySubjects.MsaSubject();
            if (requestContext.TargetCid.HasValue)
                msaSubject.Cid = requestContext.TargetCid.Value;
            msaSubject.Puid = requestContext.TargetPuid;
            msaSubject.Anid = IdConverter.AnidFromPuid((ulong)requestContext.TargetPuid);
            msaSubject.Opid = IdConverter.OpidFromPuid((ulong)requestContext.TargetPuid);

            deleteRequest.Subject = msaSubject;
            deleteRequest.Context = string.Empty;
            deleteRequest.CorrelationVector = new CorrelationVector().ToString();
            deleteRequest.IsWatchdogRequest = requestContext.IsWatchdogRequest;
            deleteRequest.RequestGuid = LogicalWebOperationContext.ServerActivityId;
            deleteRequest.RequestId = Guid.NewGuid();
            deleteRequest.RequestType = RequestType.Delete;
            deleteRequest.Timestamp = DateTimeOffset.UtcNow;
            deleteRequest.VerificationToken = null;

            // cloud instance doesn't apply to MSA
            deleteRequest.CloudInstance = null;

            // The requester must be MSA site caller name
            string requester = requestContext.GetIdentityValueOrDefault<MsaSiteIdentity, string>(i => i.CallerName);

            deleteRequest.Portal = Portals.RecurringDeleteSignal;
            deleteRequest.IsTestRequest = false;

            // update request applicability
            deleteRequest.ControllerApplicable = true;
            deleteRequest.ProcessorApplicable = false;

            return deleteRequest;
        }
    }
}


